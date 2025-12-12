using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Interactivity;


namespace Client.Common.Extensions
{ 
    /// <summary>
    /// Sets the designated property to the supplied value. TargetObject
    /// optionally designates the object on which to set the property. If
    /// TargetObject is not supplied then the property is set on the object
    /// to which the trigger is attached.
    /// </summary>
    public class SetPropertyAction : TriggerAction<FrameworkElement>
    {
        // PropertyName DependencyProperty.

        /// <summary>
        /// The property to be executed in response to the trigger.
        /// </summary>
        public string PropertyName
        {
            get { return (string)GetValue(PropertyNameProperty); }
            set { SetValue(PropertyNameProperty, value); }
        }

        public static readonly DependencyProperty PropertyNameProperty
            = DependencyProperty.Register("PropertyName", typeof(string),
            typeof(SetPropertyAction));


        // PropertyValue DependencyProperty.

        /// <summary>
        /// The value to set the property to.
        /// </summary>
        public object PropertyValue
        {
            get { return GetValue(PropertyValueProperty); }
            set { SetValue(PropertyValueProperty, value); }
        }

        public static readonly DependencyProperty PropertyValueProperty
            = DependencyProperty.Register("PropertyValue", typeof(object),
            typeof(SetPropertyAction));


        // TargetObject DependencyProperty.

        /// <summary>
        /// Specifies the object upon which to set the property.
        /// </summary>
        public object TargetObject
        {
            get { return GetValue(TargetObjectProperty); }
            set { SetValue(TargetObjectProperty, value); }
        }

        public static readonly DependencyProperty TargetObjectProperty
            = DependencyProperty.Register("TargetObject", typeof(object),
            typeof(SetPropertyAction));


        // Private Implementation.

        protected override void Invoke(object parameter)
        {
            object target = TargetObject ?? AssociatedObject;
            PropertyInfo propertyInfo = target.GetType().GetProperty(
                PropertyName,
                BindingFlags.Instance|BindingFlags.Public
                |BindingFlags.NonPublic|BindingFlags.InvokeMethod);

            propertyInfo.SetValue(target, PropertyValue);
        }
    }






    /// <summary>
    /// Sets a specified property to a value when invoked.
    /// </summary>
    public class SetterAction : TargetedTriggerAction<FrameworkElement>
    {
        #region Properties

        #region PropertyName

        /// <summary>
        /// Property that is being set by this setter.
        /// </summary>
        public string PropertyName
        {
            get { return (string)GetValue(PropertyNameProperty); }
            set { SetValue(PropertyNameProperty, value); }
        }

        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.Register("PropertyName", typeof(string), typeof(SetterAction),
            new PropertyMetadata(String.Empty));

        #endregion

        #region Value

        /// <summary>
        /// Property value that is being set by this setter.
        /// </summary>
        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(SetterAction),
            new PropertyMetadata(null));

        #endregion

        #endregion

        #region Overrides

        protected override void Invoke(object parameter)
        {
            var target = TargetObject ?? AssociatedObject;

            var targetType = target.GetType();

            var property = targetType.GetProperty(PropertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (property == null)
                throw new ArgumentException(String.Format("Property not found: {0}", PropertyName));

            if (property.CanWrite == false)
                throw new ArgumentException(String.Format("Property is not settable: {0}", PropertyName));

            object convertedValue;

            if (Value == null)
                convertedValue = null;

            else
            {
                var valueType = Value.GetType();
                var propertyType = property.PropertyType;

                if (valueType == propertyType)
                    convertedValue = Value;

                else
                {
                    var propertyConverter = TypeDescriptor.GetConverter(propertyType);

                    if (propertyConverter.CanConvertFrom(valueType))
                        convertedValue = propertyConverter.ConvertFrom(Value);

                    else if (valueType.IsSubclassOf(propertyType))
                        convertedValue = Value;

                    else
                        throw new ArgumentException(String.Format("Cannot convert type '{0}' to '{1}'.", valueType, propertyType));
                }
            }

            property.SetValue(target, convertedValue);
        }

        #endregion
    }


}
