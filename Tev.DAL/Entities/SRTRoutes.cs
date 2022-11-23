using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tev.DAL.Entities
{
    public class SRTRoutes:Entity
    {
        [Key]
        public string Id { get; set; }
        public string RouteId { get; set; }
        public int Des_Port { get; set; }
        public int Sor_Port { get; set; }
        public string PassPhrase { get; set; }
        public string GatewayIP { get; set; }
        public string Des_PortName { get; set; }
        public string Sor_PortName { get; set; }
        public string State { get; set; }
        public string? DeviceId { get; set; }
        public string RouteName { get; set; }
    }
}
