﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using RoslynDom.Common;

namespace RoslynDom
{
    // Factories are specific to the type, FactoryHelpers are specific to the level (StemMember, TypeMember, Statement, Expression)

    public abstract class RDomFactoryHelper
    {
        // WARNING: At present you must register all factories before retrieving any. So,
        private static FactoryProvider factoryProvider = new FactoryProvider();
        private static IRDomFactory<PublicAnnotation> publicAnnotationFactory;
        private static List<Tuple<Type, RDomFactoryHelper>> registration = new List<Tuple<Type, RDomFactoryHelper>>();
        //private static RDomRootFactoryHelper rootFactoryHelper;
        //private static RDomStemMemberFactoryHelper stemMemberFactoryHelper;
        //private static RDomTypeMemberFactoryHelper typeMemberFactoryHelper;
        //private static RDomStatementFactoryHelper statementFactoryHelper;
        //private static RDomExpressionFactoryHelper expressionFactoryHelper;
        //private static RDomMiscFactoryHelper miscFactoryHelper;

        //public static void Initialize()
        //{
        //    publicAnnotationFactory = factoryProvider.GetPublicAnnotationFactory();
        //}

        public static void Register<TKind>(RDomFactoryHelper<TKind> factoryHelper)
            where TKind : class, IDom
        {
            registration.Add(new Tuple<Type, RDomFactoryHelper>(typeof(TKind), factoryHelper));
        }

        public static void RegisterPublicAnnotationFactory(IRDomFactory<PublicAnnotation> factory)
        {
            publicAnnotationFactory = factory;
        }

        public static RDomFactoryHelper<TKind> GetHelper<TKind>()
            where TKind :class, IDom
        {
            foreach (var tuple in registration)
            {
                if (tuple.Item1 == typeof(TKind)) { return (RDomFactoryHelper<TKind>)tuple.Item2; }
            }
            throw new InvalidOperationException();
        }

        //public static RDomRootFactoryHelper RootFactoryHelper
        //{
        //    get
        //    {
        //        if (rootFactoryHelper == null) { rootFactoryHelper = new RDomRootFactoryHelper(); }
        //        return rootFactoryHelper;
        //    }
        //}
        //public static RDomStemMemberFactoryHelper StemMemberFactoryHelper
        //{
        //    get
        //    {
        //        if (stemMemberFactoryHelper == null) { stemMemberFactoryHelper = new RDomStemMemberFactoryHelper(); }
        //        return stemMemberFactoryHelper;
        //    }
        //}
        //public static RDomTypeMemberFactoryHelper TypeMemberFactoryHelper
        //{
        //    get
        //    {
        //        if (typeMemberFactoryHelper == null) { typeMemberFactoryHelper = new RDomTypeMemberFactoryHelper(); }
        //        return typeMemberFactoryHelper;
        //    }
        //}
        //public static RDomStatementFactoryHelper StatementFactoryHelper
        //{
        //    get
        //    {
        //        if (statementFactoryHelper == null) { statementFactoryHelper = new RDomStatementFactoryHelper(); }
        //        return statementFactoryHelper;
        //    }
        //}
        //public static RDomExpressionFactoryHelper ExpressionFactoryHelper
        //{
        //    get
        //    {
        //        if (expressionFactoryHelper == null) { expressionFactoryHelper = new RDomExpressionFactoryHelper(); }
        //        return expressionFactoryHelper;
        //    }
        //}
        //public static RDomMiscFactoryHelper MiscFactoryHelper
        //{
        //    get
        //    {
        //        if (miscFactoryHelper == null) { miscFactoryHelper = new RDomMiscFactoryHelper(); }
        //        return miscFactoryHelper;
        //    }
        //}

        //public static RDomFactoryHelper GetHelper<TKind>()
        //{
        //    if (typeof(TKind) == typeof(IRoot)) { return RootFactoryHelper; }
        //    if (typeof(TKind) == typeof(IStemMember)) { return StemMemberFactoryHelper; }
        //    if (typeof(TKind) == typeof(ITypeMember)) { return TypeMemberFactoryHelper; }
        //    if (typeof(TKind) == typeof(IStatement)) { return StatementFactoryHelper; }
        //    if (typeof(TKind) == typeof(IExpression)) { return ExpressionFactoryHelper; }
        //    if (typeof(TKind) == typeof(IMisc)) { return MiscFactoryHelper; }
        //    throw new InvalidOperationException();
        //}

        public static IEnumerable<PublicAnnotation> GetPublicAnnotations(SyntaxNode syntaxNode)
        {
            if (publicAnnotationFactory == null) { throw new InvalidOperationException(); }
            return publicAnnotationFactory.CreateFrom(syntaxNode);
        }

        protected static IEnumerable<IRDomFactory<T>> GetFactories<T>()
        {
            if (!factoryProvider.isLoaded) { factoryProvider.Initialize(registration); }
            return factoryProvider.GetFactories<T>();
        }

        public abstract SyntaxNode BuildSyntax(IDom item);
        public abstract IEnumerable<SyntaxNode> BuildSyntaxGroup(IDom item);

    }

    public abstract class RDomFactoryHelper<T> : RDomFactoryHelper
        where T : class, IDom
    {
        private IEnumerable<IRDomFactory<T>> factories;
        private IRDomFactory<T> genericFactory;
        private IEnumerable<Tuple<IRDomFactory<T>, Type, Type>> factoryLookup;

        protected IEnumerable<IRDomFactory<T>> Factories
        {
            get
            {
                if (factories == null) { factories = GetFactories<T>(); }
                return factories;
            }
        }

        private IEnumerable<Tuple<IRDomFactory<T>, Type, Type>> FactoryLookup
        {
            get
            {
                if (factoryLookup == null)
                {
                    var list = new List<Tuple<IRDomFactory<T>, Type, Type>>();
                    foreach (var factory in Factories)
                    {
                        var newTuple = new Tuple<IRDomFactory<T>, Type, Type>
                                (factory, GetSyntaxType(factory), GetTargetType(factory));
                        list.Add(newTuple);
                    }
                    factoryLookup = list;
                }
                return factoryLookup;
            }
        }

        private Type GetSyntaxType(IRDomFactory<T> factory)
        {
            var factoryType = factory.GetType();
            var syntaxType = RoslynDomUtilities.FindFirstSyntaxNodeType(factoryType);
            return syntaxType;
        }

        private Type GetTargetType(IRDomFactory<T> factory)
        {
            var factoryType = factory.GetType();
            var syntaxType = RoslynDomUtilities.FindFirstIDomType(factoryType);
            return syntaxType;
        }


        protected IRDomFactory<T> GetFactory(T item)
        {
            var type = item.GetType();
            var found = FactoryLookup.Where(x => x.Item3 == type).FirstOrDefault();
            return found.Item1;
        }

        public IEnumerable<T> MakeItem(SyntaxNode rawStatement)
        {
            var factories = Factories.OrderByDescending(x => x.Priority).ToArray();
            foreach (var factory in factories)
            {
                if (factory.CanCreateFrom(rawStatement))
                {
                    return factory.CreateFrom(rawStatement);
                }
            }
            return null;
        }

        public override IEnumerable<SyntaxNode> BuildSyntaxGroup(IDom item)
        {
            if (item == null) return null;
            var itemAsT = item as T;
            if (itemAsT == null) throw new InvalidOperationException();
            var factory = GetFactory(itemAsT);
            return factory.BuildSyntax(itemAsT);
        }

        public override SyntaxNode BuildSyntax(IDom item)
        {
            return BuildSyntaxGroup(item).Single();
        }
    }


}
