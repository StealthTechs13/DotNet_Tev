using Gremlin.Net.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Tev.Gremlin
{
    public interface ISiteDb
    {
        /// <summary>
        /// Method to check if an org exist in gremlin
        /// </summary>
        /// <param name="orgId"></param>
        /// <returns></returns>
        Task<bool> OrgExist(string orgId);


        /// <summary>
        /// Creates a new organization in cosmoms Gremlin
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<ResultSet<dynamic>> CreateOrg(string orgId, string name);

        /// <summary>
        /// Adds a child site, given a parent site
        /// </summary>
        /// <param name="parentSiteId"></param>
        /// <param name="siteName"></param>
        /// <param name="orgId"></param>
        /// <returns></returns>
        Task<ResultSet<dynamic>> AddSite(string parentSiteId, string siteName, string orgId, string siteId = null);
        
        /// <summary>
        /// Gets all the child site, given a parent site
        /// </summary>
        /// <param name="parentSite"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        Task<ResultSet<dynamic>> GetSites(string parentSite, bool recursive = false);

        /// <summary>
        /// Returns a list of siteIds down the hierarchy. SiteIds provided as input are not included
        /// </summary>
        /// <param name="orgId">OrgId, serves as partition key</param>
        /// <param name="siteIds">A list of site ids</param>
        /// <returns></returns>
        Task<List<string>> GetSiteIdsDownHierarchy(string orgId, List<string> siteIds);

        /// <summary>
        /// Returns a list of siteIds up the hierarchy. SiteIds provided as input are not included
        /// </summary>
        /// <param name="orgId">OrgId, serves as partition key</param>
        /// <param name="siteIds">A list of site ids</param>
        /// <returns></returns>
        Task<List<string>> GetSiteIdsUpHierarchy(string orgId, List<string> siteIds);

        /// <summary>
        /// Get site id and name of site down the heirarchy
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="siteIds"></param>
        /// <returns></returns>
        Task<List<string>> GetSiteIdsAndNameDownHierarchy(string orgId, List<string> siteIds);

        /// <summary>
        /// Get details of a list of sites.
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="siteIds"></param>
        /// <returns></returns>
        Task<List<string>> GetSitesDetails(string orgId, List<string> siteIds);
    }
}
