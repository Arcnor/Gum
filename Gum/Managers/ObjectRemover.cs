﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonFormsAndControls.Forms;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Debug;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;

namespace Gum.Managers
{
    public class ObjectRemover : Singleton<ObjectRemover>
    {
        public void Remove(StateSave stateSave)
        {
            bool shouldProgress = TryAskForRemovalConfirmation(stateSave, SelectedState.Self.SelectedElement);

            ElementCommands.Self.RemoveState(stateSave, SelectedState.Self.SelectedElement);
            StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedElement);
            PropertyGridManager.Self.RefreshUI();
            WireframeObjectManager.Self.RefreshAll(true);
            SelectionManager.Self.Refresh();

            ProjectVerifier.Self.AssertSelectedIpsosArePartOfRenderer();


        }

        private bool TryAskForRemovalConfirmation(StateSave stateSave, ElementSave elementSave)
        {
            bool shouldContinue = true;
            // See if the element is used anywhere

            List<InstanceSave> foundInstances = new List<InstanceSave>();

            ObjectFinder.Self.GetElementsReferencing(elementSave, null, foundInstances);

            foreach (var instance in foundInstances)
            {
                // We don't want to go recursively, just top level because
                // I *think* that the lists will include copies of the instances
                // recursively
                ElementSave parent = instance.ParentContainer;

                string variableToLookFor = instance.Name + ".State";

                // loop through all of the states to see if any of the parents' states
                // reference the state that is being removed.
                foreach (var stateInContainer in parent.States)
                {
                    var foundVariable = stateInContainer.Variables.FirstOrDefault(item => item.Name == variableToLookFor);

                    if (foundVariable != null)
                    {
                        MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                        mbmb.MessageText = "The state " + stateSave.Name + " is used in the element " + 
                            elementSave + " in its state " + stateInContainer + ".\n  What would you like to do?";

                        mbmb.AddButton("Do nothing - project may be in an invalid state", System.Windows.Forms.DialogResult.No);
                        mbmb.AddButton("Change variable to default", System.Windows.Forms.DialogResult.OK);
                        // eventually will want to add a cancel option

                        if (mbmb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            foundVariable.Value = "Default";
                        }
                    }
                }
            }

            return shouldContinue;
        }
    }
}