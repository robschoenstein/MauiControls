// Copyright © 2026 Robert Schoenstein. All rights reserved.
// Unauthorized use, reproduction, or distribution is strictly prohibited.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MauiControls.Extensions;

internal static class BindablePropertyExtension
{
    public static BindableProperty Create<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] TDeclaringType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TReturnType>(
        TReturnType? defaultValue = default,
        BindingMode defaultBindingMode = BindingMode.OneWay, BindableProperty.ValidateValueDelegate<TReturnType>? validateValue = null, BindableProperty.BindingPropertyChangedDelegate<TReturnType>? propertyChanged = null, BindableProperty.BindingPropertyChangingDelegate<TReturnType>? propertyChanging = null, BindableProperty.CoerceValueDelegate<TReturnType>? coerceValue = null, BindableProperty.CreateDefaultValueDelegate<TDeclaringType, TReturnType>? defaultValueCreator = null,
        [CallerMemberName] string propertyName = "")
    {
        if (!propertyName.EndsWith("Property", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("This extension must be used on a BindableProperty whose name is suffixed with the word 'Property'");
        }

        var trimmedPropertyName = propertyName[..^8];

        BindableProperty.ValidateValueDelegate? untypedValidateValue;
        if (validateValue != null)
        {
            untypedValidateValue = (bindable, value) => validateValue(bindable, value is TReturnType typedValue ? typedValue : default);
        }
        else
        {
            untypedValidateValue = null;
        }

        BindableProperty.BindingPropertyChangedDelegate? untypedPropertyChanged;
        if (propertyChanged != null)
        {
            untypedPropertyChanged = (bindable, o, n) => propertyChanged(bindable, o is TReturnType typedOldValue ? typedOldValue : default, n is TReturnType typedNewValue ? typedNewValue : default);
        }
        else
        {
            untypedPropertyChanged = null;
        }

        BindableProperty.BindingPropertyChangingDelegate? untypedPropertyChanging;
        if (propertyChanging != null)
        {
            untypedPropertyChanging = (bindable, o, n) => propertyChanging(bindable, o is TReturnType typedOldValue ? typedOldValue : default, n is TReturnType typedNewValue ? typedNewValue : default);
        }
        else
        {
            untypedPropertyChanging = null;
        }

        BindableProperty.CoerceValueDelegate? untypedCoerceValue;
        if (coerceValue != null)
        {
            untypedCoerceValue = (bindable, value) => coerceValue(bindable, value is TReturnType typedValue ? typedValue : default);
        }
        else
        {
            untypedCoerceValue = null;
        }

        BindableProperty.CreateDefaultValueDelegate? untypedDefaultValueCreator;
        if (defaultValueCreator != null)
        {
            untypedDefaultValueCreator = (bindable) => defaultValueCreator(bindable is TDeclaringType typedBindable ? typedBindable : default);
        }
        else
        {
            untypedDefaultValueCreator = null;
        }

        return BindableProperty.Create(
            trimmedPropertyName,
            typeof(TReturnType),
            typeof(TDeclaringType),
            defaultValue,
            defaultBindingMode,
            untypedValidateValue,
            untypedPropertyChanged,
            untypedPropertyChanging,
            untypedCoerceValue,
            untypedDefaultValueCreator);
    }
}