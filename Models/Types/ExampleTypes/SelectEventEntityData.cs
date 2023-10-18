﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using BlueprintEditor.Models.Connections;
using FrostySdk.Ebx;

namespace BlueprintEditor.Models.Types.ExampleTypes
{
    /// <summary>
    /// This is a more advanced demonstration, for a simple demonstration <see cref="CompareBoolEntityData"/>
    /// This demonstrates creating events and properties based off of the property grid
    /// </summary>
    public class SelectEventEntityData : NodeBaseModel
    {
        /// <summary>
        /// This is the name that will be displayed in the editor.
        /// This can be set to whatever you want, and can also be modified via code.
        /// </summary>
        public override string Name { get; set; } = "Select Event";
        
        /// <summary>
        /// This is the name of the type this applies to.
        /// This HAS to be the exact name of the type, so in this case, CompareBoolEntityData
        /// This value is static.
        /// </summary>
        public override string ObjectType { get; } = "SelectEventEntityData";

        /// <summary>
        /// These are all of the inputs this has.
        /// Each input allows you to customize the Title, so its name
        /// And its type, so Event, Property, and Link
        /// </summary>
        public override List<InputViewModel> Inputs { get; set; } =
            new List<InputViewModel>()
            {
                new InputViewModel() {Title = "In", Type = ConnectionType.Event},
                new InputViewModel() {Title = "Selection", Type = ConnectionType.Property}
            };

        /// <summary>
        /// These are all of the outputs this has.
        /// Each input allows you to customize the Title, so its name
        /// And its type, so Event, Property, and Link
        /// </summary>
        public override List<OutputViewModel> Outputs { get; set; } =
            new List<OutputViewModel>()
            {
            };

        /// <summary>
        /// Don't use an initializer when working with these, instead, override the OnCreation method.
        /// This triggers when the node gets created
        /// that way you can do things like change the Name based on one of its inputs, or one of the objects properties
        /// </summary>
        public override void OnCreation()
        {
            base.OnCreation(); //Always make sure to have this here. Otherwise things will break
            foreach (CString eventName in Object.Events) //Go through all of the Events this SelectEvent has
            {
                //And for each one, add it to our Outputs
                Outputs.Add(new OutputViewModel() {Title = eventName.ToString(), Type = ConnectionType.Event});
                Inputs.Add(new InputViewModel() {Title = $"Select{eventName.ToString()}", Type = ConnectionType.Event});
            }
        }
    }
}