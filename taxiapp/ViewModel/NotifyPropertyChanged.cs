using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace taxiapp.ViewModel
{
    public abstract class NotifyPropertyChanged : INotifyPropertyChanged
    {
        #region Implementation Of INotifyPropertyChanged

        /// <summary>
        /// event handler for property change
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// method for property change
        /// </summary>
        /// <param name="propertyName">name of property</param>
        public void RaiseNotifyPropertyChange([CallerMemberName] string propertyname = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }
        }
        #endregion
    }
}
