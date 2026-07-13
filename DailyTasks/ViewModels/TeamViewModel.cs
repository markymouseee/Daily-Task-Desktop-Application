using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>One selectable avatar colour in the picker.</summary>
public partial class ColorSwatch(string hex) : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public string Hex { get; } = hex;
}

/// <summary>
/// Manage the project team: add, edit and remove members with a name, role and avatar
/// colour. Deleting a member unassigns their subtasks (handled by the FK), it doesn't
/// delete work.
/// </summary>
public partial class TeamViewModel : ObservableObject
{
    // Neutral, presentation-friendly palette drawn from the app's status/accent family.
    private static readonly string[] Palette =
        ["#3B82F6", "#22C55E", "#A855F7", "#EF4444", "#F59E0B", "#14B8A6", "#EC4899", "#64748B"];

    private readonly ITeamService _team;
    private readonly int _projectId;
    private int _editingId;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editRole = string.Empty;

    [ObservableProperty]
    private string _editColor = Palette[0];

    /// <summary>Bound to the Scrum-role combo; index maps directly to <see cref="ScrumRole"/> (0 = None).</summary>
    [ObservableProperty]
    private int _scrumRoleIndex;

    [ObservableProperty]
    private bool _isEditing;

    public TeamViewModel(ITeamService team, int projectId, string projectTitle)
    {
        _team = team;
        _projectId = projectId;
        ProjectTitle = projectTitle;

        foreach (var hex in Palette)
        {
            Swatches.Add(new ColorSwatch(hex));
        }

        Swatches[0].IsSelected = true;
    }

    /// <summary>The project whose team this is, shown in the window header.</summary>
    public string ProjectTitle { get; }

    public ObservableCollection<TeamMember> Members { get; } = [];

    public ObservableCollection<ColorSwatch> Swatches { get; } = [];

    public string FormTitle => IsEditing ? "Edit member" : "Add a member";

    public string SaveLabel => IsEditing ? "Save" : "Add";

    public async Task LoadAsync()
    {
        Members.Clear();
        foreach (var member in await _team.GetForProjectAsync(_projectId))
        {
            Members.Add(member);
        }
    }

    partial void OnEditNameChanged(string value) => SaveCommand.NotifyCanExecuteChanged();

    partial void OnIsEditingChanged(bool value)
    {
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SaveLabel));
    }

    [RelayCommand]
    private void SelectColor(ColorSwatch swatch)
    {
        EditColor = swatch.Hex;
        foreach (var s in Swatches)
        {
            s.IsSelected = ReferenceEquals(s, swatch);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var role = (ScrumRole)Math.Clamp(ScrumRoleIndex, 0, 3);

        if (IsEditing)
        {
            await _team.UpdateAsync(new TeamMember
            {
                Id = _editingId,
                Name = EditName.Trim(),
                Role = EditRole.Trim(),
                InitialsColorHex = EditColor,
                ScrumRole = role,
                OwnerProjectId = _projectId,
            });
        }
        else
        {
            await _team.AddAsync(new TeamMember
            {
                Name = EditName.Trim(),
                Role = EditRole.Trim(),
                InitialsColorHex = EditColor,
                ScrumRole = role,
                OwnerProjectId = _projectId,
            });
        }

        await LoadAsync();
        ResetForm();
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(EditName);

    [RelayCommand]
    private void Edit(TeamMember member)
    {
        _editingId = member.Id;
        EditName = member.Name;
        EditRole = member.Role;
        ScrumRoleIndex = (int)member.ScrumRole;
        SelectColorHex(member.InitialsColorHex);
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit() => ResetForm();

    [RelayCommand]
    private async Task DeleteAsync(TeamMember member)
    {
        await _team.DeleteAsync(member);
        Members.Remove(member);

        if (IsEditing && _editingId == member.Id)
        {
            ResetForm();
        }
    }

    private void ResetForm()
    {
        IsEditing = false;
        _editingId = 0;
        EditName = string.Empty;
        EditRole = string.Empty;
        ScrumRoleIndex = 0;
        SelectColorHex(Palette[0]);
    }

    private void SelectColorHex(string hex)
    {
        EditColor = hex;
        foreach (var s in Swatches)
        {
            s.IsSelected = s.Hex.Equals(hex, StringComparison.OrdinalIgnoreCase);
        }
    }
}
