using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Demo.Accessors.State
{
    public class UserInfo
    {
        public GuestInfo Guest { get; set; }
        public TableInfo Table { get; set; }
        public WakeUpInfo WakeUp { get; set; }
    }
}
