using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Aura.FreeSound.API
{
	public class FreeSoundSearchOptions
	{
		public string Query
		{
			get;
			set;
		}

		public string Sort
		{
			get;
			set;
		}

		public string Filter
		{
			get;
			set;
		}

		public bool GroupByPack
		{
			get;
			set;
		}

		public int Page
		{
			get;
			set;
		}

		public int PageSize
		{
			get;
			set;
		} = 30;

		public IList<string> Fields
		{
			get;
		} = new List<string> ();

		public void AddField<T> (Expression<Func<FreeSoundInstance, T>> expression)
		{
			if (expression is null)
				throw new ArgumentNullException (nameof (expression));

			if (expression.Body is MemberExpression member) {
				Fields.Add (member.Member.Name.ToLower ());
				return;
			}

			throw new ArgumentException ();
		}

		public string GetRequest()
		{
			StringBuilder builder = new StringBuilder ("?query=");
			builder.Append (Query);

			if (!String.IsNullOrWhiteSpace (Sort)) {
				builder.Append ("&sort=");
				builder.Append (Sort);
			}

			if (!String.IsNullOrWhiteSpace (Filter)) {
				builder.Append ("&filter=");
				builder.Append (Filter);
			}

			if (GroupByPack) {
				builder.Append ("&group_by_pack=1");
			}

			if (Page > 0) {
				builder.Append ("&page=");
				builder.Append (Page);
			}

			if (PageSize > 0) {
				builder.Append ("&page_size=");
				builder.Append (PageSize);
			}

			if (Fields.Count > 0) {
				builder.Append ("&fields=");
				for (int i = 0; i < Fields.Count; i++) {
					if (i > 0)
						builder.Append (",");

					builder.Append (Fields[i]);
				}
			}

			return builder.ToString ();
		}
	}
}
