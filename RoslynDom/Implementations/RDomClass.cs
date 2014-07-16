﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynDom.Common;

namespace RoslynDom
{
    internal static class RDomClassFactoryHelper
    {
        public static void InitializeItem(RDomClass newItem, ClassDeclarationSyntax syntax)
        {
            newItem.Name = newItem.TypedSymbol.Name;
            newItem.AccessModifier = (AccessModifier)newItem.Symbol.DeclaredAccessibility;
            var newTypeParameters = newItem.TypedSymbol.TypeParametersFrom();
            foreach (var typeParameter in newTypeParameters)
            { newItem.AddOrMoveTypeParameter(typeParameter); }
            var members = ListUtilities.MakeList(newItem.TypedSyntax, x => x.Members, x => RDomFactoryHelper.GetHelper<ITypeMember>().MakeItem(x));
            foreach (var member in members)
            { newItem.AddOrMoveMember(member); }
            newItem.BaseType = new RDomReferencedType(newItem.TypedSymbol.DeclaringSyntaxReferences, newItem.TypedSymbol.BaseType);
            newItem.IsAbstract = newItem.Symbol.IsAbstract;
            newItem.IsSealed = newItem.Symbol.IsSealed;
            newItem.IsStatic = newItem.Symbol.IsStatic;
        }

        public static IEnumerable<SyntaxNode> BuildSyntax(RDomClass item)
        {
            var modifiers = item.BuildModfierSyntax();
            var identifier = SyntaxFactory.Identifier(item.Name);
            var node = SyntaxFactory.ClassDeclaration(identifier)
                .WithModifiers(modifiers);
            var itemAsClass = item as IClass;
            if (itemAsClass == null) { throw new InvalidOperationException(); }
            var attributes = BuildSyntaxExtensions.BuildAttributeListSyntax(item.Attributes);
            if (attributes.Any()) { node = node.WithAttributeLists(attributes); }
            var membersSyntax = itemAsClass.Members
                        .SelectMany(x => RDomFactory.BuildSyntaxGroup(x))
                        .ToList();
            node = node.WithMembers(SyntaxFactory.List(membersSyntax));
            // TODO: Class type members and type constraints
            return new SyntaxNode[] { RoslynUtilities.Format(node) };
        }
    }
    public class RDomClassTypeMemberFactory
           : RDomTypeMemberFactory<RDomClass, ClassDeclarationSyntax>
    {
        public override void InitializeItem(RDomClass newItem, ClassDeclarationSyntax syntax)
        {
            RDomClassFactoryHelper.InitializeItem(newItem, syntax);
        }
        public override IEnumerable<SyntaxNode> BuildSyntax(ITypeMember item)
        {
            return RDomClassFactoryHelper.BuildSyntax((RDomClass)item);
        }
    }

    public class RDomClassStemMemberFactory
           : RDomStemMemberFactory<RDomClass, ClassDeclarationSyntax>
    {
        public override void InitializeItem(RDomClass newItem, ClassDeclarationSyntax syntax)
        {
            RDomClassFactoryHelper.InitializeItem(newItem, syntax);
        }
        public override IEnumerable<SyntaxNode> BuildSyntax(IStemMember item)
        {
            return RDomClassFactoryHelper.BuildSyntax((RDomClass)item);
        }
    }



    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// I'm not currently supporting parameters (am supporting type parameters) because I don't understand them
    /// </remarks>
    public class RDomClass : RDomBaseType<IClass, ClassDeclarationSyntax>, IClass
    {
        internal RDomClass(ClassDeclarationSyntax rawItem)
             : base(rawItem, MemberKind.Class, StemMemberKind.Class)
        {
            //Initialize2();
        }

        //internal RDomClass(ClassDeclarationSyntax rawItem,
        //       IEnumerable<ITypeMember> members,
        //       params PublicAnnotation[] publicAnnotations)
        //       : base(rawItem, MemberKind.Class, StemMemberKind.Class, members, publicAnnotations)
        //{
        //    Initialize();
        //}

        internal RDomClass(RDomClass oldRDom)
             : base(oldRDom)
        {
            BaseType = oldRDom.BaseType.Copy();
            IsAbstract = oldRDom.IsAbstract;
            IsSealed = oldRDom.IsSealed;
            IsStatic = oldRDom.IsStatic;
        }

        //protected override void Initialize()
        //{
        //    base.Initialize();
        //    BaseType = new RDomReferencedType(TypedSymbol.DeclaringSyntaxReferences, TypedSymbol.BaseType);
        //    IsAbstract = Symbol.IsAbstract;
        //    IsSealed = Symbol.IsSealed;
        //    IsStatic = Symbol.IsStatic;
        //}

        //private void Initialize2()
        //{
        //    Initialize();
        //    var members = ListUtilities.MakeList(TypedSyntax, x => x.Members, x => RDomFactoryHelper.TypeMemberFactoryHelper.MakeItem(x));
        //    foreach (var member in members)
        //    { AddOrMoveMember(member); }
        //}

        //public override ClassDeclarationSyntax BuildSyntax()
        //{
        //    var modifiers = this.BuildModfierSyntax();
        //    var node = SyntaxFactory.ClassDeclaration(Name)
        //                    .WithModifiers(modifiers);

        //    node = RoslynUtilities.UpdateNodeIfListNotEmpty(BuildMembers(true), node, (n, l) => n.WithMembers(l));
        //    //node = RoslynUtilities.UpdateNodeIfListNotEmpty(BuildTypeParameterList(), node, (n, l) => n.WithTypeParameters(l));
        //    //node = RoslynUtilities.UpdateNodeIfListNotEmpty(BuildConstraintClauses(), node, (n, l) => n.WithTypeConstraints(l));
        //    node = RoslynUtilities.UpdateNodeIfListNotEmpty(BuildAttributeListSyntax(), node, (n, l) => n.WithAttributeLists(l));

        //    return (ClassDeclarationSyntax)RoslynUtilities.Format(node);
        //}


        public IEnumerable<IClass> Classes
        { get { return Members.OfType<IClass>(); } }

        public IEnumerable<IType> Types
        //{ get { return Classes.Concat<IStemMember>(Structures).Concat(Interfaces).Concat(Enums); } }
        { get { return Members.OfType<IType>(); } }


        public IEnumerable<IStructure> Structures
        {
            get
            { return Members.OfType<IStructure>(); }
        }

        public IEnumerable<IInterface> Interfaces
        {
            get
            { return Members.OfType<IInterface>(); }
        }

        public IEnumerable<IEnum> Enums
        {
            get
            { return Members.OfType<IEnum>(); }
        }

        public bool IsAbstract { get; set; }

        public bool IsSealed { get; set; }

        public bool IsStatic { get; set; }

        public IReferencedType BaseType { get; set; }

        public IEnumerable<IReferencedType> ImplementedInterfaces
        {
            get
            {
                return this.ImpementedInterfacesFrom(false);
            }
        }

        public IEnumerable<IReferencedType> AllImplementedInterfaces
        { get { return this.ImpementedInterfacesFrom(true); } }

        public string ClassName { get { return this.Name; } }
    }
}
