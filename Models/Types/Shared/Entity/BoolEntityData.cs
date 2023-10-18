using System.Collections.Generic;
using System.Collections.ObjectModel;
using BlueprintEditor.Models.Connections;

namespace BlueprintEditor.Models.Types.Shared.Entity
{
    public class BoolEntityData : NodeBaseModel
    {
        public override string Name { get; set; } = "Bool";
        public override string ObjectType { get; } = "BoolEntityData";

        public override List<InputViewModel> Inputs { get; set; } =
            new List<InputViewModel>()
            {
                new InputViewModel() {Title = "Value", Type = ConnectionType.Property},
                new InputViewModel() {Title = "SetTrue", Type = ConnectionType.Event},
                new InputViewModel() {Title = "SetFalse", Type = ConnectionType.Event}
            };

        public override List<OutputViewModel> Outputs { get; set; } =
            new List<OutputViewModel>()
            {
                new OutputViewModel() {Title = "Value", Type = ConnectionType.Property}
            };
    }
}