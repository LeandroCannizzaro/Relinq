// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// as published by the Free Software Foundation; either version 2.1 of the 
// License, or (at your option) any later version.
// 
// re-motion is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-motion; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using Remotion.Data.Linq.Clauses.Expressions;
using Remotion.Data.Linq.Utilities;

namespace Remotion.Data.Linq.Parsing
{
  /// <summary>
  /// Provides a base class that can be used for visiting and optionally transforming each node of an <see cref="Expression"/> tree in a 
  /// strongly typed fashion.
  /// This is the base class of many transformation classes.
  /// </summary>
  public abstract class ExpressionTreeVisitor
  {
    public virtual Expression VisitExpression (Expression expression)
    {
      if (expression == null)
        return null;

      var extensionExpression = expression as ExtensionExpression;
      if (extensionExpression != null)
        return extensionExpression.Accept (this);

      switch (expression.NodeType)
      {
        case ExpressionType.ArrayLength:
        case ExpressionType.Convert:
        case ExpressionType.ConvertChecked:
        case ExpressionType.Negate:
        case ExpressionType.NegateChecked:
        case ExpressionType.Not:
        case ExpressionType.Quote:
        case ExpressionType.TypeAs:
        case ExpressionType.UnaryPlus:
          return VisitUnaryExpression ((UnaryExpression) expression);
        case ExpressionType.Add:
        case ExpressionType.AddChecked:
        case ExpressionType.Divide:
        case ExpressionType.Modulo:
        case ExpressionType.Multiply:
        case ExpressionType.MultiplyChecked:
        case ExpressionType.Power:
        case ExpressionType.Subtract:
        case ExpressionType.SubtractChecked:
        case ExpressionType.And:
        case ExpressionType.Or:
        case ExpressionType.ExclusiveOr:
        case ExpressionType.LeftShift:
        case ExpressionType.RightShift:
        case ExpressionType.AndAlso:
        case ExpressionType.OrElse:
        case ExpressionType.Equal:
        case ExpressionType.NotEqual:
        case ExpressionType.GreaterThanOrEqual:
        case ExpressionType.GreaterThan:
        case ExpressionType.LessThan:
        case ExpressionType.LessThanOrEqual:
        case ExpressionType.Coalesce:
        case ExpressionType.ArrayIndex:
          return VisitBinaryExpression ((BinaryExpression) expression);
        case ExpressionType.Conditional:
          return VisitConditionalExpression ((ConditionalExpression) expression);
        case ExpressionType.Constant:
          return VisitConstantExpression ((ConstantExpression) expression);
        case ExpressionType.Invoke:
          return VisitInvocationExpression ((InvocationExpression) expression);
        case ExpressionType.Lambda:
          return VisitLambdaExpression ((LambdaExpression) expression);
        case ExpressionType.MemberAccess:
          return VisitMemberExpression ((MemberExpression) expression);
        case ExpressionType.Call:
          return VisitMethodCallExpression ((MethodCallExpression) expression);
        case ExpressionType.New:
          return VisitNewExpression ((NewExpression) expression);
        case ExpressionType.NewArrayBounds:
        case ExpressionType.NewArrayInit:
          return VisitNewArrayExpression ((NewArrayExpression) expression);
        case ExpressionType.MemberInit:
          return VisitMemberInitExpression ((MemberInitExpression) expression);
        case ExpressionType.ListInit:
          return VisitListInitExpression ((ListInitExpression) expression);
        case ExpressionType.Parameter:
          return VisitParameterExpression ((ParameterExpression) expression);
        case ExpressionType.TypeIs:
          return VisitTypeBinaryExpression ((TypeBinaryExpression) expression);

        default:
          if (expression is SubQueryExpression)
            return VisitSubQueryExpression ((SubQueryExpression) expression);
          else if (expression is QuerySourceReferenceExpression)
            return VisitQuerySourceReferenceExpression ((QuerySourceReferenceExpression) expression);
          else
            return VisitUnknownExpression (expression);
      }
    }

    protected internal virtual Expression VisitUnknownExpression (Expression expression)
    {
      var extensionExpression = expression as ExtensionExpression;
      if (extensionExpression != null)
        return extensionExpression.VisitChildren (this);
      else
      {
        var message = string.Format ("Expression type {0} is not supported by this {1}.", expression.NodeType, GetType().Name);
        throw new NotSupportedException (message);
      }
    }

    protected virtual Expression VisitUnaryExpression (UnaryExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      Expression newOperand = VisitExpression (expression.Operand);
      if (newOperand != expression.Operand)
      {
        if (expression.NodeType == ExpressionType.UnaryPlus)
          return Expression.UnaryPlus (newOperand, expression.Method);
        else
          return Expression.MakeUnary (expression.NodeType, newOperand, expression.Type, expression.Method);
      }
      else
        return expression;
    }

    protected virtual Expression VisitBinaryExpression (BinaryExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      Expression newLeft = VisitExpression (expression.Left);
      Expression newRight = VisitExpression (expression.Right);
      var newConversion = (LambdaExpression) VisitExpression (expression.Conversion);
      if (newLeft != expression.Left || newRight != expression.Right || newConversion != expression.Conversion)
        return Expression.MakeBinary (expression.NodeType, newLeft, newRight, expression.IsLiftedToNull, expression.Method, newConversion);
      return expression;
    }

    protected virtual Expression VisitTypeBinaryExpression (TypeBinaryExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      Expression newExpression = VisitExpression (expression.Expression);
      if (newExpression != expression.Expression)
        return Expression.TypeIs (newExpression, expression.TypeOperand);
      return expression;
    }

    protected virtual Expression VisitConstantExpression (ConstantExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      return expression;
    }

    protected virtual Expression VisitConditionalExpression (ConditionalExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      Expression newTest = VisitExpression (expression.Test);
      Expression newFalse = VisitExpression (expression.IfFalse);
      Expression newTrue = VisitExpression (expression.IfTrue);
      if ((newTest != expression.Test) || (newFalse != expression.IfFalse) || (newTrue != expression.IfTrue))
        return Expression.Condition (newTest, newTrue, newFalse);
      return expression;
    }

    protected virtual Expression VisitParameterExpression (ParameterExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      return expression;
    }

    protected virtual Expression VisitLambdaExpression (LambdaExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      ReadOnlyCollection<ParameterExpression> newParameters = VisitExpressionList (expression.Parameters, "VisitLambdaExpression");
      Expression newBody = VisitExpression (expression.Body);
      if ((newBody != expression.Body) || (newParameters != expression.Parameters))
        return Expression.Lambda (expression.Type, newBody, newParameters);
      return expression;
    }

    protected virtual Expression VisitMethodCallExpression (MethodCallExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      Expression newObject = VisitExpression (expression.Object);
      ReadOnlyCollection<Expression> newArguments = VisitExpressionList (expression.Arguments, "VisitMethodCallExpression");
      if ((newObject != expression.Object) || (newArguments != expression.Arguments))
        return Expression.Call (newObject, expression.Method, newArguments);
      return expression;
    }

    protected virtual Expression VisitInvocationExpression (InvocationExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      Expression newExpression = VisitExpression (expression.Expression);
      ReadOnlyCollection<Expression> newArguments = VisitExpressionList (expression.Arguments, "VisitInvocationExpression");
      if ((newExpression != expression.Expression) || (newArguments != expression.Arguments))
        return Expression.Invoke (newExpression, newArguments);
      return expression;
    }

    protected virtual Expression VisitMemberExpression (MemberExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      Expression newExpression = VisitExpression (expression.Expression);
      if (newExpression != expression.Expression)
        return Expression.MakeMemberAccess (newExpression, expression.Member);
      return expression;
    }

    protected virtual Expression VisitNewExpression (NewExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      ReadOnlyCollection<Expression> newArguments = VisitExpressionList (expression.Arguments, "VisitNewExpression");
      if (newArguments != expression.Arguments)
      {
        if (expression.Members == null)
          return Expression.New (expression.Constructor, newArguments);
        else
          return Expression.New (expression.Constructor, newArguments, expression.Members);
      }
      return expression;
    }

    protected virtual Expression VisitNewArrayExpression (NewArrayExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      ReadOnlyCollection<Expression> newExpressions = VisitExpressionList (expression.Expressions, "VisitNewArrayExpression");
      if (newExpressions != expression.Expressions)
      {
        var elementType = expression.Type.GetElementType();
        if (expression.NodeType == ExpressionType.NewArrayInit)
          return Expression.NewArrayInit (elementType, newExpressions);
        else
          return Expression.NewArrayBounds (elementType, newExpressions);
      }
      return expression;
    }

    protected virtual T VisitAndConvert<T> (T expression, string methodName) where T: Expression
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      ArgumentUtility.CheckNotNull ("methodName", methodName);

      var newExpression = VisitExpression (expression);

      if ((newExpression as T) == null)
      {
        var message = string.Format (
            "When called from '{0}', expressions of type '{1}' can only be replaced with other expressions of type '{2}'.",
            methodName,
            newExpression.GetType().Name,
            typeof (T).Name);
       
        throw new InvalidOperationException (message);
      }
      return (T) newExpression;
    }

    protected virtual Expression VisitMemberInitExpression (MemberInitExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      var newNewExpression = VisitExpression (expression.NewExpression) as NewExpression;
      if (newNewExpression == null)
      {
        throw new NotSupportedException (
            "MemberInitExpressions only support non-null instances of type 'NewExpression' as their NewExpression member.");
      }

      ReadOnlyCollection<MemberBinding> newBindings = VisitMemberBindingList (expression.Bindings);
      if (newNewExpression != expression.NewExpression || newBindings != expression.Bindings)
        return Expression.MemberInit (newNewExpression, newBindings);
      return expression;
    }

    protected virtual Expression VisitListInitExpression (ListInitExpression expression)
    {
      ArgumentUtility.CheckNotNull ("expression", expression);
      var newNewExpression = VisitExpression (expression.NewExpression) as NewExpression;
      if (newNewExpression == null)
        throw new NotSupportedException ("ListInitExpressions only support non-null instances of type 'NewExpression' as their NewExpression member.");
      ReadOnlyCollection<ElementInit> newInitializers = VisitElementInitList (expression.Initializers);
      if (newNewExpression != expression.NewExpression || newInitializers != expression.Initializers)
        return Expression.ListInit (newNewExpression, newInitializers);
      return expression;
    }

    protected virtual ElementInit VisitElementInit (ElementInit elementInit)
    {
      ArgumentUtility.CheckNotNull ("elementInit", elementInit);
      ReadOnlyCollection<Expression> newArguments = VisitExpressionList (elementInit.Arguments, "VisitElementInit");
      if (newArguments != elementInit.Arguments)
        return Expression.ElementInit (elementInit.AddMethod, newArguments);
      return elementInit;
    }

    protected virtual MemberBinding VisitMemberBinding (MemberBinding memberBinding)
    {
      ArgumentUtility.CheckNotNull ("memberBinding", memberBinding);
      switch (memberBinding.BindingType)
      {
        case MemberBindingType.Assignment:
          return VisitMemberAssignment ((MemberAssignment) memberBinding);
        case MemberBindingType.MemberBinding:
          return VisitMemberMemberBinding ((MemberMemberBinding) memberBinding);
        default:
          Debug.Assert (
              memberBinding.BindingType == MemberBindingType.ListBinding, "Invalid member binding type " + memberBinding.GetType().FullName);
          return VisitMemberListBinding ((MemberListBinding) memberBinding);
      }
    }

    protected virtual MemberBinding VisitMemberAssignment (MemberAssignment memberAssigment)
    {
      ArgumentUtility.CheckNotNull ("memberAssigment", memberAssigment);

      Expression expression = VisitExpression (memberAssigment.Expression);
      if (expression != memberAssigment.Expression)
        return Expression.Bind (memberAssigment.Member, expression);
      return memberAssigment;
    }

    protected virtual MemberBinding VisitMemberMemberBinding (MemberMemberBinding binding)
    {
      ArgumentUtility.CheckNotNull ("binding", binding);

      ReadOnlyCollection<MemberBinding> newBindings = VisitMemberBindingList (binding.Bindings);
      if (newBindings != binding.Bindings)
        return Expression.MemberBind (binding.Member, newBindings);
      return binding;
    }

    protected virtual MemberBinding VisitMemberListBinding (MemberListBinding listBinding)
    {
      ArgumentUtility.CheckNotNull ("listBinding", listBinding);
      ReadOnlyCollection<ElementInit> newInitializers = VisitElementInitList (listBinding.Initializers);

      if (newInitializers != listBinding.Initializers)
        return Expression.ListBind (listBinding.Member, newInitializers);
      return listBinding;
    }

    public virtual ReadOnlyCollection<T> VisitExpressionList<T> (ReadOnlyCollection<T> expressions, string callerName) where T: Expression
    {
       return VisitList (expressions, expression => VisitAndConvert (expression, callerName));
    }

    protected virtual ReadOnlyCollection<MemberBinding> VisitMemberBindingList (ReadOnlyCollection<MemberBinding> expressions)
    {
      return VisitList (expressions, VisitMemberBinding);
    }

    protected virtual ReadOnlyCollection<ElementInit> VisitElementInitList (ReadOnlyCollection<ElementInit> expressions)
    {
      return VisitList (expressions, VisitElementInit);
    }

    protected virtual Expression VisitSubQueryExpression (SubQueryExpression expression)
    {
      return expression;
    }

    protected virtual Expression VisitQuerySourceReferenceExpression (QuerySourceReferenceExpression expression)
    {
      return expression;
    }

    public ReadOnlyCollection<T> VisitList<T> (ReadOnlyCollection<T> list, Func<T, T> visitMethod)
        where T : class
    {
      List<T> newList = null;

      for (int i = 0; i < list.Count; i++)
      {
        T element = list[i];
        T newElement = visitMethod (element);
        if (newElement == null)
          throw new NotSupportedException ("The current list only supports objects of type '" + typeof (T).Name + "' as its elements.");

        if (element != newElement)
        {
          if (newList == null)
            newList = new List<T> (list);

          newList[i] = newElement;
        }
      }

      if (newList != null)
        return newList.AsReadOnly ();
      else
        return list;
    }
  }
}