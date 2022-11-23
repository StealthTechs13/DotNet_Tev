using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Gremlin.Net.Driver;
using System.Linq;

namespace Tev.Gremlin
{
    /// <summary>
    /// Label for devices is 'device'
    /// Label for sites is 'site'
    /// Label for organizations is 'org'
    /// </summary>
    public class SiteDb : ISiteDb
    {
        private GremlinClient gremlinClient;
        public SiteDb(ITevGremlinClient client)
        {
            gremlinClient = client.GremlinClient;
        }

        public async Task<bool> OrgExist(string orgId)
        {
            var result = await gremlinClient.SubmitAsync<dynamic>($"g.V('{orgId}').hasLabel('org')");
            if (result.Count == 0)
                return false;
            return true;
        }
        public async Task<ResultSet<dynamic>> CreateOrg(string orgId, string name)
        {
            var result = await gremlinClient.SubmitAsync<dynamic>($"g.addV('org').property('id', '{orgId}').property('orgId','{orgId}').property('name','{name}')");
            return result;
        }

        public async Task<ResultSet<dynamic>> AddSite(string parentSiteId,string siteName,string orgId,string siteId=null)
        {
            if (string.IsNullOrEmpty(siteId))
            {
                var result = await gremlinClient.SubmitAsync<dynamic>($"g.addV('site').property('orgId','{orgId}').property('name','{siteName}').as('m').V('{parentSiteId}').addE('child').to('m')");
                return result;
            }
            else
            {
                var result = await gremlinClient.SubmitAsync<dynamic>($"g.addV('site').property('id','{siteId}').property('orgId','{orgId}').property('name','{siteName}').as('m').V('{parentSiteId}').addE('child').to('m')");
                return result;
            }
            
        }

        
        public async Task<ResultSet<dynamic>> GetSites(string parentSite, bool recursive = false)
        {
            if (!recursive) 
            {
                var result1 = await gremlinClient.SubmitAsync<dynamic>($"g.V('{parentSite}').out('child').hasLabel('site')");
                return result1;
            }
            var result = await gremlinClient.SubmitAsync<dynamic>($"g.V('{parentSite}').repeat(out('child').hasLabel('site')).emit()");
            return result;
        }

        public async Task<List<string>> GetSiteIdsDownHierarchy(string orgId, List<string> siteIds)
        {
            if (siteIds.Count == 0)
            {
                return new List<string>();
            }
            var querySite = new List<string>();
            foreach (var siteId in siteIds)
            {
                querySite.Add($"['{orgId}','{siteId}']");
            }
            var querySiteString = string.Join(',', querySite);
            var result = await gremlinClient.SubmitAsync<dynamic>($"g.V({querySiteString}).repeat(out('child').hasLabel('site')).emit().values('id')");
            var ret = result.Select(x => (string)x).ToList();
            return ret;
        }

        public async Task<List<string>> GetSiteIdsAndNameDownHierarchy(string orgId, List<string> siteIds)
        {
            if (siteIds.Count == 0)
            {
                return new List<string>();
            }
            var querySite = new List<string>();
            foreach (var siteId in siteIds)
            {
                querySite.Add($"['{orgId}','{siteId}']");
            }
            var querySiteString = string.Join(',', querySite);
            var result = await gremlinClient.SubmitAsync<dynamic>($"g.V({querySiteString}).repeat(out('child').hasLabel('site')).emit().values('id','name')");
            var ret = result.Select(x => (string)x).ToList();
            return ret;
        }

        public async Task<List<string>> GetSitesDetails(string orgId, List<string> siteIds)
        {
            if (siteIds.Count == 0)
            {
                return new List<string>();
            }
            var querySite = new List<string>();
            foreach (var siteId in siteIds)
            {
                querySite.Add($"['{orgId}','{siteId}']");
            }
            var querySiteString = string.Join(',', querySite);
            var result = await gremlinClient.SubmitAsync<dynamic>($"g.V({querySiteString}).values('id','name')");
            var ret = result.Select(x => (string)x).ToList();
            return ret;
        }

        public async Task<List<string>> GetSiteIdsUpHierarchy(string orgId, List<string> siteIds)
        {
            if (siteIds.Count == 0)
            {
                return new List<string>();
            }
            var querySite = new List<string>();
            foreach (var siteId in siteIds)
            {
                querySite.Add($"['{orgId}','{siteId}']");
            }
            var querySiteString = string.Join(',', querySite);
            var result = await gremlinClient.SubmitAsync<dynamic>($"g.V({querySiteString}).repeat(__.in('child').hasLabel('site')).emit().values('id')");
            var ret = result.Select(x => (string)x).ToList();
            return ret;
        }
    }
}   
