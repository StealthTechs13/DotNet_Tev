using Microsoft.AspNetCore.Authorization;
using MMSConstants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.AuthPolicies
{
    public class RoleHandler : AuthorizationHandler<RoleRequirement>
    {

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
        {
            if (context != null && requirement != null)
            {
                var roles = context.User.Claims.Where(x => x.Type == MMSClaimTypes.Role).ToList();
                foreach (var claim in roles)
                {
                    if (claim.Value == "HoneywellAdmin")
                    {
                        context.Succeed(requirement);
                        return Task.CompletedTask;
                    }
                    var roleName = claim.Value.Split("-")[1];
                    if (requirement.Role.Contains(roleName))
                    {
                        context.Succeed(requirement);
                        return Task.CompletedTask;
                    }
                }
                context.Fail();
                return Task.CompletedTask;
            }
            else
            {
                throw new ArgumentNullException("context Or requirement");
            }

        }
    }

    public class RoleRequirement : IAuthorizationRequirement
    {
        public List<string> Role { get; private set; }
        public RoleRequirement(List<string> role)
        {
            Role = role;
        }

    }
}
