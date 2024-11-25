using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

/// <summary>
/// Editor for the manual library
/// </summary>
public partial class Libraries
{
    /// <summary>
    /// Opens the manual library eidotr
    /// </summary>
    /// <param name="library">the manual library</param>
    /// <returns>true if the editor was saved, otherwise false</returns>
    private async Task<bool> OpenManualLibraryEditor(Library library)
    {
        List<IFlowField> fields = new ();
        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(library.Name),
            Parameters = new Dictionary<string, object>()
            {
                { nameof(InputText.ReadOnly) , true }
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(library.Priority),
            Parameters = new Dictionary<string, object>{
                { "AllowClear", false },
                { "Options", new List<ListOption> {
                    new () { Value = ProcessingPriority.Lowest, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Lowest)}" },
                    new () { Value = ProcessingPriority.Low, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Low)}" },
                    new () { Value = ProcessingPriority.Normal, Label =$"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Normal)}" },
                    new () { Value = ProcessingPriority.High, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.High)}" },
                    new () { Value = ProcessingPriority.Highest, Label = $"Enums.{nameof(ProcessingPriority)}.{nameof(ProcessingPriority.Highest)}" }
                } }
            }
        });
        
        // if(Profile.LicensedFor(LicenseFlags.ProcessingOrder))
        // {
        //     fields.Add(new ElementField
        //     {
        //         InputType = FormInputType.Select,
        //         Name = nameof(library.ProcessingOrder),
        //         Parameters = new Dictionary<string, object>{
        //             { "AllowClear", false },
        //             { "Options", new List<ListOption> {
        //                 new () { Value = ProcessingOrder.AsFound, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.AsFound)}" },
        //                 new () { Value = ProcessingOrder.Alphabetical, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.Alphabetical)}" },
        //                 new () { Value = ProcessingOrder.SmallestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.SmallestFirst)}" },
        //                 new () { Value = ProcessingOrder.LargestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.LargestFirst)}" },
        //                 new () { Value = ProcessingOrder.NewestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.NewestFirst)}" },
        //                 new () { Value = ProcessingOrder.OldestFirst, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.OldestFirst)}" },
        //                 new () { Value = ProcessingOrder.Random, Label = $"Enums.{nameof(ProcessingOrder)}.{nameof(ProcessingOrder.Random)}" },
        //             } }
        //         }
        //     });
        // }
        if (Profile.LicensedFor(LicenseFlags.ProcessingOrder))
        {
            fields.Add(new ElementField()
            {
                InputType = FormInputType.Int,
                Name = nameof(library.MaxRunners)
            });
        }
        
        await Editor.Open(new()
        {
            TypeName = "Pages.Library", Title = "Pages.Library.Title", Model = library, SaveCallback = Save, Fields = fields,
            HelpUrl = "https://fileflows.com/docs/webconsole/configuration/libraries/library"
        });
        return true;
    }
}