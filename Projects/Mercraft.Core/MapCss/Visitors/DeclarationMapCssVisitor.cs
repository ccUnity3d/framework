﻿using System;
using Antlr.Runtime.Tree;
using Mercraft.Core.MapCss.Domain;
using Mercraft.Core.MapCss.Visitors.Eval;

namespace Mercraft.Core.MapCss.Visitors
{
    public class DeclarationMapCssVisitor: MapCssVisitorBase
    {
        private readonly bool _canUseExprTree;

        public DeclarationMapCssVisitor(bool canUseExprTree)
        {
            _canUseExprTree = canUseExprTree;
        }

        public override Declaration VisitDeclaration(CommonTree declarationTree)
        {
            var declaration = new Declaration();
            if (declarationTree == null)
            {
                throw new MapCssFormatException(declarationTree, "Declaration tree not valid!");
            }

            declaration.Qualifier = String.Intern(declarationTree.Children[0].Text);
            declaration.Value = String.Intern(declarationTree.Children[1].Text);

            if (declaration.Value == "EVAL_CALL")
            {
                declaration.IsEval = true;
                declaration.Evaluator =_canUseExprTree ?
                     (ITreeWalker) new ExpressionEvalTreeWalker(declarationTree.Children[1] as CommonTree) :
                     (ITreeWalker) new StringEvalTreeWalker(declarationTree.Children[1] as CommonTree);
            }

            if (declaration.Value == "VALUE_RGB")
            {
                declaration.IsEval = true;
                declaration.Evaluator = new ColorTreeWalker(declarationTree.Children[1] as CommonTree);
            }

            if (declaration.Value == "VALUE_LIST")
            {
                declaration.IsEval = true;
                declaration.Evaluator = new ListTreeWalker(declarationTree.Children[1] as CommonTree);
            }

            return declaration;
        }
    }
}
