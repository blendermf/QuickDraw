using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickDraw.Utilities;

public class MFNotifyCollectionChangedEventArgs : NotifyCollectionChangedEventArgs
{
    public bool FromModel = false;
    public MFNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, bool fromModel) : base(action)
    {
        FromModel = fromModel;
    }

    public MFNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems, bool fromModel) : base(action, changedItems)
    {
        FromModel = fromModel;
    }

    public MFNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem, bool fromModel) : base(action, changedItem)
    {
        FromModel = fromModel;
    }

    public MFNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems, bool fromModel) : base(action, newItems, oldItems)
    {
        FromModel = fromModel;
    }

    public MFNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems, int startingIndex, bool fromModel) : base(action, changedItems, startingIndex)
    {
        FromModel = fromModel;
    }

    public MFNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem, int index, bool fromModel) : base(action, changedItem, index)
    {
        FromModel = fromModel;
    }

    public MFNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object newItem, object oldItem, bool fromModel) : base(action, newItem, oldItem)
    {
        FromModel = fromModel;
    }

    public MFNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems, int startingIndex, bool fromModel) : base(action, newItems, oldItems, startingIndex)
    {
        FromModel = fromModel;
    }

    public MFNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems, int index, int oldIndex, bool fromModel) : base(action, changedItems, index, oldIndex)
    {
        FromModel = fromModel;
    }

    public MFNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem, int index, int oldIndex, bool fromModel) : base(action, changedItem, index, oldIndex)
    {
        FromModel = fromModel;
    }

    public MFNotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object newItem, object oldItem, int index, bool fromModel) : base(action, newItem, oldItem, index)
    {
        FromModel = fromModel;
    }
}
