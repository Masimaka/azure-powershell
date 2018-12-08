﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Management.Automation;
using System.Net;
using Microsoft.Azure.Commands.FrontDoor.Common;
using Microsoft.Azure.Commands.FrontDoor.Helpers;
using Microsoft.Azure.Commands.FrontDoor.Models;
using Microsoft.Azure.Commands.FrontDoor.Properties;
using Microsoft.Azure.Management.FrontDoor;
using Microsoft.WindowsAzure.Commands.Utilities.Common;
using System.Linq;
using Microsoft.Azure.Commands.ResourceManager.Common.ArgumentCompleters;
using Microsoft.Azure.Commands.ResourceManager;
using Microsoft.Azure.Management.Internal.Resources.Utilities.Models;
using SdkPolicy = Microsoft.Azure.Management.FrontDoor.Models.WebApplicationFirewallPolicy1;
namespace Microsoft.Azure.Commands.FrontDoor.Cmdlets
{
    /// <summary>
    /// Defines the Set-AzFrontDoorFireWallPolicy cmdlet.
    /// </summary>
    [Cmdlet("Set", ResourceManager.Common.AzureRMConstants.AzureRMPrefix + "FrontDoorFireWallPolicy", SupportsShouldProcess = true, DefaultParameterSetName = FieldsParameterSet), OutputType(typeof(PSPolicy))]
    public class SetAzureRmFrontDoorFireWallPolicy : AzureFrontDoorCmdletBase
    {
        /// <summary>
        /// The resource group to which the FireWallPolicy belongs.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = FieldsParameterSet, HelpMessage = "The resource group to which the FireWallPolicy belongs.")]
        [ValidateNotNullOrEmpty]
        public string ResourceGroupName { get; set; }

        /// <summary>
        /// The name of the FireWallPolicy to update.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = FieldsParameterSet, HelpMessage = "The name of the FireWallPolicy to update.")]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        ///The FireWallPolicy object to update.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = ObjectParameterSet, ValueFromPipeline = true, HelpMessage = "The FireWallPolicy object to update.")]
        [ValidateNotNullOrEmpty]
        public PSPolicy InputObject { get; set; }

        /// <summary>
        /// Resource Id of the FireWallPolicy to update
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = ResourceIdParameterSet, ValueFromPipelineByPropertyName = true, HelpMessage = "Resource Id of the FireWallPolicy to update")]
        [ValidateNotNullOrEmpty]
        public string ResourceId { get; set; }

        /// <summary>
        /// Whether the policy is in enabled state or disabled state. Possible values include: 'Disabled', 'Enabled'
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Whether the policy is in enabled state or disabled state. Possible values include: 'Disabled', 'Enabled'")]
        public PSEnabledState EnabledState { get; set; }

        /// <summary>
        /// Describes if it is in detection mode  or prevention mode at policy level. Possible values include:'Prevention', 'Detection'
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Describes if it is in detection mode  or prevention mode at policy level. Possible values include:'Prevention', 'Detection'")]
        public PSMode Mode { get; set; }

        /// <summary>
        /// Custom rules inside the policy
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Custom rules inside the policy")]
        public PSCustomRule[] Customrule { get; set; }

        /// <summary>
        /// Managed rules inside the policy
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Managed rules inside the policy")]
        public PSManagedRule[] ManagedRule { get; set; }



        public override void ExecuteCmdlet()
        {
            if (ParameterSetName == ObjectParameterSet)
            {
                ResourceIdentifier identifier = new ResourceIdentifier(InputObject.Id);
                ResourceGroupName = identifier.ResourceGroupName;
                Name = InputObject.Name;
            }
            else if (ParameterSetName == ResourceIdParameterSet)
            {
                ResourceIdentifier identifier = new ResourceIdentifier(ResourceId);
                ResourceGroupName = identifier.ResourceGroupName;
                Name = InputObject.Name;
            }


            var existingPolicy = FrontDoorManagementClient.Policies.List(ResourceGroupName)
                .FirstOrDefault(x => x.Name.ToLower() == Name.ToLower());


            if (existingPolicy == null)
            {
                throw new PSArgumentException(string.Format(
                    Resources.Error_WebApplicationFirewallPolicyNotFound,
                    Name,
                    ResourceGroupName));
            }

            SdkPolicy updateParameters;
            if (ParameterSetName == ObjectParameterSet)
            {
                updateParameters = InputObject.ToSdkFirewallPolicy();
            }
            else
            {
                updateParameters = existingPolicy;
            }

            if (this.IsParameterBound(c => c.EnabledState))
            {
                updateParameters.PolicySettings.EnabledState = EnabledState.ToString();
            }

            if (this.IsParameterBound(c => c.Mode))
            {
                updateParameters.PolicySettings.Mode = Mode.ToString();
            }

            if (this.IsParameterBound(c => c.Customrule))
            {
                updateParameters.CustomRules = new Management.FrontDoor.Models.CustomRules
                {
                    Rules = Customrule.ToList().Select(x => x.ToSdkCustomRule()).ToList()
                };
            }

            if (this.IsParameterBound(c => c.ManagedRule))
            {
                updateParameters.ManagedRules = new Management.FrontDoor.Models.ManagedRuleSets
                {
                    RuleSets = ManagedRule.ToList().Select(x => x.ToSdkAzManagedRule()).ToList()
                };
            }
            if (ShouldProcess(Resources.WebApplicationFirewallPolicyTarget, string.Format(Resources.WebApplicationFirewallPolicyChangeWarning, Name)))
            {
                try
                {
                    var policy = FrontDoorManagementClient.Policies.CreateOrUpdate(
                                    ResourceGroupName,
                                    Name,
                                    updateParameters
                                    );
                    WriteObject(policy.ToPSPolicy());
                }
                catch (Microsoft.Azure.Management.FrontDoor.Models.ErrorResponseException e)
                {
                    throw new PSArgumentException(string.Format(
                        Resources.Error_ErrorResponseFromServer,
                        e.Response.Content));
                }
            }
        }
    }
}
