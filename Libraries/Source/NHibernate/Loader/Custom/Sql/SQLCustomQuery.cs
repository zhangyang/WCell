using System.Collections;
using Iesi.Collections;
using log4net;
using NHibernate.Engine;
using NHibernate.Engine.Query.Sql;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.SqlCommand;
using NHibernate.Util;
using System.Collections.Generic;
using Iesi.Collections.Generic;

namespace NHibernate.Loader.Custom.Sql
{
	public class SQLCustomQuery : ICustomQuery
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(SQLCustomQuery));

		private readonly SqlString sql;
		private readonly Iesi.Collections.Generic.ISet<string> querySpaces = new HashedSet<string>();
		private readonly IDictionary namedParameterBindPoints = new Hashtable();
		private readonly IList customQueryReturns = new ArrayList();

		public SqlString SQL
		{
			get { return sql; }
		}

		public IDictionary NamedParameterBindPoints
		{
			get { return namedParameterBindPoints; }
		}

		public Iesi.Collections.Generic.ISet<string> QuerySpaces
		{
			get { return querySpaces; }
		}

		public IList CustomQueryReturns
		{
			get { return customQueryReturns; }
		}

		public SQLCustomQuery(
			INativeSQLQueryReturn[] queryReturns,
			string sqlQuery,
			ICollection<string> additionalQuerySpaces,
			ISessionFactoryImplementor factory)
		{
			log.Debug("starting processing of sql query [" + sqlQuery + "]");
			SQLQueryReturnProcessor processor = new SQLQueryReturnProcessor(queryReturns, factory);
			SQLQueryReturnProcessor.ResultAliasContext aliasContext = processor.Process();

			SQLQueryParser parser = new SQLQueryParser(sqlQuery, new ParserContext(aliasContext));
			sql = parser.Process();
			ArrayHelper.AddAll(this.namedParameterBindPoints, parser.NamedParameters);
			ArrayHelper.AddAll(customQueryReturns, processor.GenerateCustomReturns(parser.QueryHasAliases));

			if (additionalQuerySpaces != null)
			{
				querySpaces.AddAll(additionalQuerySpaces);
			}
		}

		private class ParserContext : SQLQueryParser.IParserContext
		{
			private readonly SQLQueryReturnProcessor.ResultAliasContext aliasContext;

			public ParserContext(SQLQueryReturnProcessor.ResultAliasContext aliasContext)
			{
				this.aliasContext = aliasContext;
			}

			public bool IsEntityAlias(string alias)
			{
				return GetEntityPersisterByAlias(alias) != null;
			}

			public ISqlLoadable GetEntityPersisterByAlias(string alias)
			{
				return aliasContext.GetEntityPersister(alias);
			}

			public string GetEntitySuffixByAlias(string alias)
			{
				return aliasContext.GetEntitySuffix(alias);
			}

			public bool IsCollectionAlias(string alias)
			{
				return GetCollectionPersisterByAlias(alias) != null;
			}

			public ISqlLoadableCollection GetCollectionPersisterByAlias(string alias)
			{
				return aliasContext.GetCollectionPersister(alias);
			}

			public string GetCollectionSuffixByAlias(string alias)
			{
				return aliasContext.GetCollectionSuffix(alias);
			}

			public IDictionary GetPropertyResultsMapByAlias(string alias)
			{
				return aliasContext.GetPropertyResultsMap(alias);
			}
		}
	}
}