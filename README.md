# MauiControls Library
This library contains controls I needed and didn't want to pay for... So, I wrote my own. Microslop REALLY dropped the ball on this.

**This is a WIP and functionality could change in the future.**
## How to use:
Just look at the examples. It's pretty self-explanatory.

### ToggleButton

#### Basic Usage (Text):
```xml
<controls:ToggleButton
        Checked="{Binding IsChecked, Mode=TwoWay}"
        CheckedText="Checked"
        UncheckedText="Unchecked"
        Command="{Binding ToggleCommand}" />
```

#### Advanced Usage (Custom Content)
```xml
<controls:ToggleButton
        Checked="{Binding IsChecked, Mode=TwoWay}"
        Command="{Binding ToggleCommand}">

    <controls:ToggleButton.CheckedContent>
        <Image Source="checked_icon.png"
               WidthRequest="28" HeightRequest="28" />
    </controls:ToggleButton.CheckedContent>

    <controls:ToggleButton.UncheckedContent>
        <Image Source="unchecked_icon.png"
               WidthRequest="28" HeightRequest="28" />
    </controls:ToggleButton.UncheckedContent>
</controls:ToggleButton>
```

#### Available Properties:
| Property                 | Type         | Default     | Description                                    |
|--------------------------|--------------|-------------|------------------------------------------------|
| Checked                  | bool         | false       | Current toggle state (TwoWay binding)          |
| CheckedText              | string       | "On"        | Text shown when checked (if no custom content)~~~~ |
| UncheckedText            | string       | "Off"       | Text shown when unchecked                      |
| CheckedContent           | View         | null        | Custom content when checked                    | 
| UncheckedContent         | View         | null        | Custom content when unchecked                  |
| CheckedBackgroundColor   | Color        | Green       | Background color when checked                  |
| UncheckedBackgroundColor | Color        | Gray        | Background color when unchecked                |
| CheckedTextColor         | Color        | White       | Text color when checked                        |
| UncheckedTextColor       | Color        | Black       | Text color when unchecked                      |
| CornerRadius             | CornerRadius | 8           | Button corner radius                           |
| BorderColor              | Color        | Transparent | Color of the border                            |
| BorderThickness          | Thickness    | 0           | Thickness of the border                        |
| Padding                  | Thickness    | 12,6        | Internal padding of the control                |
| FontSize                 | double       | 14.0        | Font size used for the fallback label          |
| Command                  | ICommand     | null        | Executed when toggled                          |
| CommandParameter         | object       | null        | Parameter passed to Command                    |

#### ViewModel:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Maui.ViewModels
{
    public partial class ToggleButonViewModel
    {
        [ObservableProperty]
        private bool _isChecked;

        //ctor and whatever else...
        
        [RelayCommand]
        private void Toggle()
        {
            // Optional logic when toggle changes
            Console.WriteLine($"Toggle state changed to: {IsChecked}");
        }
    }
}
```
### ComboBox
A ComboBox control.

#### ComboBox Entry Style:
```xml
<Style x:Key="ComboBoxEntryStyle" TargetType="Entry">
    <Setter Property="BackgroundColor" Value="White" />
    <Setter Property="TextColor" Value="Black" />
    <Setter Property="FontSize" Value="16" />
    <Setter Property="Padding" Value="12,10" />
</Style>
```

#### Usage:
```xml
<controls:ComboBox 
    ItemsSource="{Binding Customers}"
    SelectedItem="{Binding SelectedCustomer}"
    EntryDisplayPath="FullName"
    Placeholder="Type to search customers..."
    ListViewHeightRequest="220"
    EntryStyle="{StaticResource ComboBoxEntryStyle}"
    IsDropDownOpen="{Binding IsDropDownOpen, Mode=TwoWay}" />

<Label Text="Selected Customer:" FontAttributes="Bold" />
<Label Text="{Binding SelectedCustomer.FullName}" />
```

#### Available Properties:
| Property                | Type        | Default      | Description                                    |
|-------------------------|-------------|--------------|------------------------------------------------|
| ItemsSource             | IEnumerable | null         | Collection of items to display in the dropdown |
| SelectedItem            | object      | null         | Currently selected item (TwoWay)               |
| Text                    | string      | null         | Two-way bound text value of the Entry          |
| EntryDisplayPath        | string      | string.Empty | Property name to display for each item         |
| Placeholder             | string      | string.Empty | Placeholder text shown in the Entry            |
| IsDropDownOpen          | bool        | false        | Controls visibility of the dropdown (TwoWay)   |
| DebounceMilliseconds    | int         | 300          | Delay before filtering the list                |
| IsReadOnly              | bool        | false        | Makes the Entry read-only                      |
| IsClearButtonVisible    | bool        | true         | Shows/hides the automatic clear (×) button     |
| EntryStyle              | Style       | null         | Style applied to the internal Entry            |
| CollectionViewStyle     | Style       | null         | Style applied to the dropdown CollectionView   |
| ClearButtonStyle        | Style       | null         | Style applied to the clear button              |
| DropDownBackgroundColor | Color       | White        | Background color of the dropdown               |

#### ViewModel:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Models; // Assuming your shared models live here
using System.Collections.ObjectModel;

namespace Maui.ViewModels
{
    public partial class CustomerComboBoxViewModel
    {
        //Before you ask, CustomerDto is a data Model. You should know this.
        [ObservableProperty]
        private ObservableCollection<CustomerDto> _customers = new();

        [ObservableProperty]
        private CustomerDto? _selectedCustomer;

        [ObservableProperty]
        private bool _isDropDownOpen;

        //ctor and whatever else...
        
        public CustomerComboBoxViewModel()
        {
            // Sample data
            Customers.Add(new CustomerDto { Id = Guid.NewGuid(), FullName = "John Smith", Email = "john.smith@marklar.com" });
            Customers.Add(new CustomerDto { Id = Guid.NewGuid(), FullName = "Sarah Johnson", Email = "sarah.j@marklar.com" });
            Customers.Add(new CustomerDto { Id = Guid.NewGuid(), FullName = "Michael Rodriguez", Email = "mrodriguez@marklar.com" });
            Customers.Add(new CustomerDto { Id = Guid.NewGuid(), FullName = "Emily Chen", Email = "emily.chen@marklar.com" });
        }
    }
}
```

## DataGrid
A pretty robust DataGrid control that supports paging, sorting, filtering, and editing.

#### Basic usage:
```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:mc="clr-namespace:MauiControls;assembly=MauiControls"
             x:Class="Client.Views.ManageUsersView"
             x:DataType="vm:ManageUsersViewModel">
    <ContentPage.Content>
        <mc:DataGrid x:Name="UsersDataGrid"
                     ItemsSource="{Binding Users}"
                     SelectionMode="Single"
                     FilteringEnabled="False"
                     SelectedItem="{Binding SelectedUser}"
                     PaginationEnabled="True"
                     SelectionChangedCommand="{Binding OnUserSelectedCommand}"
                     SelectionChangedCommandParameter="{Binding SelectedUser.Id}">
            <mc:DataGrid.Columns>
                <mc:DataGridColumn Title="First Name"
                                   PropertyName="FirstName"
                                   SortingEnabled="True"
                                   FilteringEnabled="True"
                                   ToolTipProperties.Text="How do you not know what First Name means?"/>
                <mc:DataGridColumn Title="Last Name"
                                   PropertyName="LastName"
                                   SortingEnabled="True"
                                   FilteringEnabled="True" />
                <mc:DataGridColumn Title="User Name"
                                   PropertyName="UserName"
                                   SortingEnabled="True"
                                   FilteringEnabled="True" />
                <mc:DataGridColumn Title="Account Active"
                                   PropertyName="IsActive"
                                   SortingEnabled="True"
                                   FilteringEnabled="False"
                                   ToolTipProperties.Text="Is user account active."/>
                <mc:DataGridColumn Title="Account Lockout"
                                   PropertyName="LockoutEnabled"
                                   SortingEnabled="True"
                                   FilteringEnabled="False"
                                   ToolTipProperties.Text="Is user currently locked out due to invalid login attempts."/>
            </mc:DataGrid.Columns>
        </mc:DataGrid>
    </ContentPage.Content>
</ContentPage>
```

#### Available Properties:
| Property                         | Type                                 | Default | Description                                           |
|----------------------------------|--------------------------------------|---------|-------------------------------------------------------|
| ItemsSource                      | IEnumerable                          | null    | Data source for the grid                              |
| Columns                          | ObservableCollection<DataGridColumn> | (empty) | Collection of column definitions                      |
| SelectedItem                     | object                               | null    | Currently selected item (TwoWay)                      |
| SelectedItems                    | IList<object>                        | (empty) | Selected items when SelectionMode = Multiple (TwoWay) |
| SelectionMode                    | SelectionMode                        | Single  | None / Single / Multiple                              |
| SelectionChangedCommand          | ICommand                             | null    | MVVM command executed on selection change             |
| SelectionChangedCommandParameter | object                               | null    | Parameter passed to SelectionChangedCommand           |
| SortingEnabled                   | bool                                 | true    | Enables sorting on the entire grid                    |
| FilteringEnabled                 | bool                                 | false   | Enables filtering on the entire grid                  |
| PaginationEnabled                | bool                                 | false   | Enables built-in pagination controls                  |
| PageSize                         | int                                  | 10      | Number of rows per page                               |
| PageNumber                       | int                                  | 1       | Current page number (TwoWay)                          |
| RowHeight                        | int                                  | 40      | Height of each data row                               |
| HeaderHeight                     | int                                  | 40      | Height of the header row                              |
| FooterHeight                     | int                                  | 40      | Height of the footer (pagination) row                 |
| ActiveRowColor                   | Color                                | #8090A0 | Background color of the selected row                  |
| HeaderBackground                 | Color                                | White   | Background color of the header                        |
| FooterBackground                 | Color                                | White   | Background color of the footer                        |
| BorderColor                      | Color                                | Black   | Color of cell borders                                 |
| BorderThickness                  | Thickness                            | 1       | Thickness of cell borders                             |
| PullToRefreshCommand             | ICommand                             | null    | Command executed on pull-to-refresh                   |
| IsRefreshing                     | bool                                 | false   | TwoWay binding for the refresh indicator              |

#### ViewModel
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Models; // Assuming your shared models live here
using System.Collections.ObjectModel;

namespace Maui.ViewModels
{
    public partial class UserDataGridViewModel
    {
        //Before you ask, CustomerDto is a data Model. You should know this.
        [ObservableProperty]
        private ObservableCollection<UserDto> _users = new();
                
         [ObservableProperty]
         private UserDto? _selectedUser;
             
         //ctor and whatever else...
             
        [RelayCommand]
        private void OnUserSelected(object? parameter)
        {
            if (parameter is int id)
            {
                // Handle row selection
            }
        }
    }
}
```