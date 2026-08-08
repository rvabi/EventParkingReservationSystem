using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Common;

namespace EventParking.Models.Entities;

public class Venue : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public int TotalCapacity { get; set; }

    public ICollection<Event> Events { get; set; }
        = new List<Event>();

    public ICollection<VenueFacility> Facilities { get; set; }
    = new List<VenueFacility>();
}
