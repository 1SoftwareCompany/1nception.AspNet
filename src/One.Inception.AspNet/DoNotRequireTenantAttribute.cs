using System;

namespace One.Inception.AspNet
{
    /// <summary>
    /// An attribute which can be used to specify that the current request does not require a tenant
    /// Keep in mind that <see cref="One.Inception.MessageProcessing.InceptionContext"/>  requires tenant, so use at your own discretion
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class BypassTenantAttribute : Attribute
    {
    }
}
