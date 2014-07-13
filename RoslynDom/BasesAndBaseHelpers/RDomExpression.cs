﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynDom.Common;

namespace RoslynDom
{
    public class RDomExpression : RDomBase<IExpression, ExpressionSyntax, ISymbol>, IExpression
    {
        internal RDomExpression(ExpressionSyntax rawItem)
           : base(rawItem)
        {
            Expression = rawItem.ToString();
        }

        public string Expression { get; set; }
    }
}