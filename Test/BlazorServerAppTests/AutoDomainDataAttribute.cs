using AutoFixture;
using AutoFixture.AutoMoq;

namespace BlazorServerAppTests
{
    public class AutoDomainDataAttribute : AutoDataAttribute
    {
        /// <summary>
        /// 
        /// </summary>
        public AutoDomainDataAttribute()
            : base(() =>
            {
                // Create the fixture
                var fix = new Fixture();
                fix.Customize(new AutoMoqCustomization());
                
                // Configure so it does not automatically set values to public setters.
                fix.OmitAutoProperties = true;

                return fix;
            })
        {
        }
    }
}
