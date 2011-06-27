// This file is part of the re-linq project (relinq.codeplex.com)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// re-linq is free software; you can redistribute it and/or modify it under 
// the terms of the GNU Lesser General Public License as published by the 
// Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
// 
// re-linq is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-linq; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq.Parsing.Structure;
using Remotion.Linq.Utilities;

namespace Remotion.Linq
{
  /// <summary>
  /// Acts as a common base class for <see cref="IQueryable{T}"/> implementations based on re-linq. In a specific LINQ provider, a custom queryable
  /// class should be derived from <see cref="QueryableBase{T}"/> which supplies an implementation of <see cref="IQueryExecutor"/> that is used to 
  /// execute the query. This is then used as an entry point (the main data source) of a LINQ query.
  /// </summary>
  /// <typeparam name="T">The type of the result items yielded by this query.</typeparam>
  public abstract class QueryableBase<T> : IOrderedQueryable<T>
  {
    private readonly IQueryProvider _queryProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryableBase&lt;T&gt;"/> class with a <see cref="DefaultQueryProvider"/> and the given
    /// <paramref name="executor"/>. This constructor should be used by subclasses to begin a new query. The <see cref="Expression"/> generated by
    /// this constructor is a <see cref="ConstantExpression"/> pointing back to this <see cref="QueryableBase{T}"/>.
    /// </summary>
    /// <param name="queryParser">The <see cref="IQueryParser"/> used to parse queries. Specify an instance of 
    ///   <see cref="QueryParser"/> for default behavior. See also <see cref="QueryParser.CreateDefault"/>.</param>
    /// <param name="executor">The <see cref="IQueryExecutor"/> used to execute the query represented by this <see cref="QueryableBase{T}"/>.</param>
    protected QueryableBase (IQueryParser queryParser, IQueryExecutor executor)
    {
      ArgumentUtility.CheckNotNull ("executor", executor);
      ArgumentUtility.CheckNotNull ("queryParser", queryParser);

      _queryProvider = new DefaultQueryProvider (GetType().GetGenericTypeDefinition(), queryParser, executor);
      Expression = Expression.Constant (this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryableBase&lt;T&gt;"/> class with a specific <see cref="IQueryProvider"/>. This constructor
    /// should only be used to begin a query when <see cref="DefaultQueryProvider"/> does not fit the requirements.
    /// </summary>
    /// <param name="provider">The provider used to execute the query represented by this <see cref="QueryableBase{T}"/> and to construct
    /// queries around this <see cref="QueryableBase{T}"/>.</param>
    protected QueryableBase (IQueryProvider provider)
    {
      ArgumentUtility.CheckNotNull ("provider", provider);

      _queryProvider = provider;
      Expression = Expression.Constant (this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryableBase&lt;T&gt;"/> class with a given <paramref name="provider"/> and 
    /// <paramref name="expression"/>. This is an infrastructure constructor that must be exposed on subclasses because it is used by 
    /// <see cref="DefaultQueryProvider"/> to construct queries around this <see cref="QueryableBase{T}"/> when a query method (e.g. of the
    /// <see cref="Queryable"/> class) is called.
    /// </summary>
    /// <param name="provider">The provider used to execute the query represented by this <see cref="QueryableBase{T}"/> and to construct
    /// queries around this <see cref="QueryableBase{T}"/>.</param>
    /// <param name="expression">The expression representing the query.</param>
    protected QueryableBase (IQueryProvider provider, Expression expression)
    {
      ArgumentUtility.CheckNotNull ("provider", provider);
      ArgumentUtility.CheckNotNull ("expression", expression);

      if (!typeof (IEnumerable<T>).IsAssignableFrom (expression.Type))
        throw new ArgumentTypeException ("expression", typeof (IEnumerable<T>), expression.Type);

      _queryProvider = provider;
      Expression = expression;
    }

    /// <summary>
    /// Gets the expression tree that is associated with the instance of <see cref="T:System.Linq.IQueryable"/>. This expression describes the
    /// query represented by this <see cref="QueryableBase{T}"/>.
    /// </summary>
    /// <value></value>
    /// <returns>
    /// The <see cref="T:System.Linq.Expressions.Expression"/> that is associated with this instance of <see cref="T:System.Linq.IQueryable"/>.
    /// </returns>
    public Expression Expression { get; private set; }

    /// <summary>
    /// Gets the query provider that is associated with this data source. The provider is used to execute the query. By default, a 
    /// <see cref="DefaultQueryProvider"/> is used that parses the query and passes it on to an implementation of <see cref="IQueryExecutor"/>.
    /// </summary>
    /// <value></value>
    /// <returns>
    /// The <see cref="T:System.Linq.IQueryProvider"/> that is associated with this data source.
    /// </returns>
    public IQueryProvider Provider
    {
      get { return _queryProvider; }
    }

    /// <summary>
    /// Gets the type of the element(s) that are returned when the expression tree associated with this instance of <see cref="T:System.Linq.IQueryable"/> is executed.
    /// </summary>
    /// <value></value>
    /// <returns>
    /// A <see cref="T:System.Type"/> that represents the type of the element(s) that are returned when the expression tree associated with this object is executed.
    /// </returns>
    public Type ElementType
    {
      get { return typeof (T); }
    }

    /// <summary>
    /// Executes the query via the <see cref="Provider"/> and returns an enumerator that iterates through the items returned by the query.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the query result.
    /// </returns>
    public IEnumerator<T> GetEnumerator ()
    {
      return _queryProvider.Execute<IEnumerable<T>> (Expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator ()
    {
      return ((IEnumerable) _queryProvider.Execute (Expression)).GetEnumerator();
    }
  }
}
