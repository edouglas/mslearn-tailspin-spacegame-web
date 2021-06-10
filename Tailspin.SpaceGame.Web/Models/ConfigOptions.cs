using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tailspin.SpaceGame.Web.Models
{
    public class ConfigOptions
    {
        public string ConfigVar { get; set; }
        public string EnvVar { get; set; }
        public string SecretVar { get; set; }
    }
}
