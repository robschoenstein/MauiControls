# MauiControls Library
This library contains controls I needed and didn't want to pay for... So, I wrote my own. Microslop REALLY dropped the ball on this.

**This is a WIP and functionality could change in the future.**
## How to use:
Just look at the examples. It's pretty self-explanatory.

### ToggleButton~~~~

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
| Property | Type     | Default | Description                                    |
| -------- |----------|---------|------------------------------------------------|
| Checked | bool     | false   | Current toggle state (TwoWay binding)          |
| CheckedText | string   | "On"    | Text shown when checked (if no custom content) |
| UncheckedText | string   | "Off"   | Text shown when unchecked                      |
| CheckedContent | View     | null    | Custom content when checked                    | 
| UncheckedContent | View     | null    | Custom content when unchecked                  |
| CheckedBackgroundColor | Color    | Green   | Background color when checked |
| UncheckedBackgroundColor | Color    | Gray    | Background color when unchecked |
| CheckedTextColor | Color    | White   | Text color when checked |
| UncheckedTextColor | Color    | Black   | Text color when unchecked |
| CornerRadius | CornerRadius | 8 | Button corner radius |
| Command | ICommand | null    | Executed when toggled |
| CommandParameter | object   | null    | Parameter passed to Command |

#### ViewModel:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Maui.ViewModels
{
    public partial class ToggleButonViewModel
    {
        [ObservableProperty]
        private bool isChecked;

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
I did this one in a hurry and still need to finish fleshing out the styling.

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

#### ViewModel:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Models;           // Assuming your shared models live here
using System.Collections.ObjectModel;

namespace Maui.ViewModels
{
    public partial class CustomerComboBoxViewModel
    {
        //Before you ask, CustomerDto is a data Model. You should know this.
        [ObservableProperty]
        private ObservableCollection<CustomerDto> customers = new();

        [ObservableProperty]
        private CustomerDto? selectedCustomer;

        [ObservableProperty]
        private bool isDropDownOpen;

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
I borrowed some of the code from another repo... I don't remember where, but I'll give them credit when I do.

There are a LOT more options that I don't currently have time to document.

#### Basic usage:
```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:Client.Views"
             xmlns:mc="clr-namespace:MauiControls;assembly=MauiControls"
             x:Class="Client.Views.ManageUsersView"
             x:DataType="vm:ManageUsersViewModel">
    <ContentPage.Content>        
        <mc:DataGrid x:Name="UsersDataGrid"
                           ItemsSource="{Binding Users}"
                           SelectionMode="Single"
                           FilteringEnabled="False"
                           SelectedItem="{Binding SelectedUser}"
                           PaginationEnabled="True">
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