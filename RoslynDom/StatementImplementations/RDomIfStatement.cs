using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using RoslynDom.Common;
using System.ComponentModel.DataAnnotations;

namespace RoslynDom
{
   public class RDomIfStatement : RDomStatementBlockBase<IIfStatement>, IIfStatement
   {
      private RDomCollection<IElseBaseStatement> _elses;

      public RDomIfStatement(IExpression  condition)
          : this(null, null, null)
      {
         NeedsFormatting = true;
         Condition = condition;
      }

      public RDomIfStatement(SyntaxNode rawItem, IDom parent, SemanticModel model)
         : base(rawItem, parent, model)
      { Initialize(); }

      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
         "CA1811:AvoidUncalledPrivateCode", Justification = "Called via Reflection")]
      internal RDomIfStatement(RDomIfStatement oldRDom)
          : base(oldRDom)
      {
         Initialize();
         var newElses = RoslynDomUtilities.CopyMembers(oldRDom.Elses);
         Elses.AddOrMoveRange(newElses);
         Condition = oldRDom.Condition.Copy();
      }

      private void Initialize()
      {
         _elses = new RDomCollection<IElseBaseStatement>(this);
      }

      public override IEnumerable<IDom> Children
      {
         get
         {
            var list = new List<IDom>();
            list.Add(Condition);
            list.AddRange(base.Children.ToList());
            list.AddRange(Elses);
            return list;
         }
      }

      private IExpression _condition;
      public IExpression Condition
      {
         get { return _condition; }
         set { SetProperty(ref _condition, value); }
      }

      public RDomCollection<IElseBaseStatement> Elses
      { get { return _elses; } }

      [Required]
      public IFinalElseStatement Else
      {
         get
         {
            var candidates = Elses.OfType<IFinalElseStatement>();
            if (candidates.Count() == 0)
            { return null; }
            else if (candidates.Count() == 1)
            { return candidates.First(); }
            else
            { throw new InvalidOperationException(); }
         }
      }

      public IEnumerable<IElseIfStatement> ElseIfs
      { get { return Elses.OfType<IElseIfStatement>().ToList(); } }
   }
}
