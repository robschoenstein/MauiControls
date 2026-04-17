# MauiControls Library
This library contains controls I needed and didn't want to pay for... So, I wrote my own. Microslop REALLY dropped the ball on this.~~~~

## How to use:
Just look at the examples. It's pretty self-explanatory.

### ToggleButton
```xml
<controls:ToggleButton 
    IsToggled="{Binding IsEnabled}"
    OnText="Enabled"
    OffText="Disabled"
    OnBackgroundColor="Green"
    OffBackgroundColor="Gray"
    Command="{Binding ToggleCommand}" />
```

### ComboBox
I did this one in a hurry and still need to finish fleshing out the styling. For now...

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

#### ViewModel
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Maui.ViewModels.Base;
using Shared.Models;           // Assuming your shared models live here
using System.Collections.ObjectModel;

namespace Maui.ViewModels
{
    public partial class CustomerComboBoxViewModel : ViewModelBase
    {
        //Before you ask, CustomerDto is a data Model. You should know this.
        [ObservableProperty]
        private ObservableCollection<CustomerDto> customers = new();

        [ObservableProperty]
        private CustomerDto? selectedCustomer;

        [ObservableProperty]
        private bool isDropDownOpen;

        public CustomerComboBoxViewModel(INavigationService navigationService)
            : base(navigationService)
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

**This is a WIP and functionality could change in the future.**

There are a LOT more options that I haven't had a chance to document.

#### Basic usage:~~~~
```xml
<dg:DataGrid x:Name="UsersDataGrid"
                   ItemsSource="{Binding Users}"
                   SelectionMode="Single"
                   FilteringEnabled="False"
                   SelectedItem="{Binding SelectedUser}"
                   PaginationEnabled="True">
    <dg:DataGrid.Columns>
        <dg:DataGridColumn Title="First Name"
                           PropertyName="FirstName"
                           SortingEnabled="True"
                           FilteringEnabled="True"
                           ToolTipProperties.Text="How do you not know what First Name means?"/>
        <dg:DataGridColumn Title="Last Name"
                           PropertyName="LastName"
                           SortingEnabled="True"
                           FilteringEnabled="True" />
        <dg:DataGridColumn Title="User Name"
                           PropertyName="UserName"
                           SortingEnabled="True"
                           FilteringEnabled="True" />
        <dg:DataGridColumn Title="Account Active"
                           PropertyName="IsActive"
                           SortingEnabled="True"
                           FilteringEnabled="False"
                           ToolTipProperties.Text="Is user account active."/>
        <dg:DataGridColumn Title="Account Lockout"
                           PropertyName="LockoutEnabled"
                           SortingEnabled="True"
                           FilteringEnabled="False" 
                           ToolTipProperties.Text="Is user currently locked out due to invalid login attempts."/>
    </dg:DataGrid.Columns>
</dg:DataGrid>
```