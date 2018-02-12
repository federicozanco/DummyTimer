using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace DummyTimer
{
    public class IdLabelValue : INotifyPropertyChanged
    {
        #region Constants
        public const long UndefinedId = -1L;
        #endregion

        #region Private fields
        private long? _id;
        private string _label;
        private object _value;
        #endregion

        #region Public properties
        public long? Id
        {
            get { return _id; }
            set
            {
                _id = value;

                NotifyPropertyChanged(() => Id);
            }
        }

        public virtual string Label
        {
            get { return _label; }
            set
            {
                _label = value;

                NotifyPropertyChanged(() => Label);
            }
        }

        public virtual object Value
        {
            get { return _value; }
            set
            {
                this._value = value;

                NotifyPropertyChanged(() => Value);
            }
        }
        #endregion

        #region INotifyPropertyChanged
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged == null)
                return;

            PropertyChanged(this, new PropertyChangedEventArgs(info));
        }

        public void NotifyPropertyChanged<T>(Expression<Func<T>> expr)
        {
            NotifyPropertyChanged((expr.Body as MemberExpression).Member.Name);
        }
        #endregion

        #region ToString
        public override string ToString()
        {
            return "[" + _id + "] " + _label + " = " + _value;
        }
        #endregion
    }
}
