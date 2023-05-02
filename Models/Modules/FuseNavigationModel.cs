using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApi.Models.Modules
{
    public class FuseNavigationModel
    {
        public string id { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string icon { get; set; }
        public List<FuseNavigationDetailModel> children { get; set; }
    }

    public class FuseNavigationDetailModel
    {
        public string id { get; set; }
        public string title { get; set; }
        public string translate { get; set; }
        public string type { get; set; }
        public string icon { get; set; }
        public List<FuseNavigationDetail2Model> children { get; set; }
    }

    public class FuseNavigationDetail2Model
    {
        public string id { get; set; }
        public string title { get; set; }
        public string translate { get; set; }
        public string type { get; set; }
        public string icon { get; set; }
        public string url { get; set; }
    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> enumerable, Func<T, TKey> keySelector)
        {
            return enumerable.GroupBy(keySelector).Select(grp => grp.First());
        }
    }
}