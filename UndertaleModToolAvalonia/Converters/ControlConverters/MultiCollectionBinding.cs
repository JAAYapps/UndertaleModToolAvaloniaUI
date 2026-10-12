using Avalonia.Data.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;

namespace UndertaleModToolAvalonia.Converters.ControlConverters
{
    public class MultiCollectionBinding : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            ObservableCollection<object> collection = new();

            void Refill()
            {
                collection.Clear();
                foreach (var v in values)
                    if (v is IEnumerable enumerable)
                        foreach (object item in enumerable)
                            collection.Add(item);
            }

            foreach (var v in values)
                if (v is INotifyCollectionChanged notifying)
                    notifying.CollectionChanged += (s, e) => Refill();

            Refill();
            return collection;
        }
    }
}
