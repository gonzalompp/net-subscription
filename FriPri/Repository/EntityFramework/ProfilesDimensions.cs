//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Repository.EntityFramework
{
    using System;
    using System.Collections.Generic;
    
    public partial class ProfilesDimensions
    {
        public int IdProfileDimension { get; set; }
        public Nullable<int> IdProfile { get; set; }
        public Nullable<int> IdDimension { get; set; }
        public Nullable<decimal> Value { get; set; }
        public Nullable<bool> SwitchValue { get; set; }
        public Nullable<bool> Active { get; set; }
        public Nullable<bool> IsInfinite { get; set; }
    
        public virtual Dimensions Dimensions { get; set; }
        public virtual Profiles Profiles { get; set; }
    }
}
