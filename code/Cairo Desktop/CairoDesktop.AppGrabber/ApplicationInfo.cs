﻿using System;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Imaging;

namespace CairoDesktop.AppGrabber
{
    [Serializable()]
    public class ApplicationInfo : IEquatable<ApplicationInfo>, IComparable<ApplicationInfo>, INotifyPropertyChanged
	{
        /// <summary>
        /// This object holds the basic information necessary for identifying an application.
        /// </summary>
		public ApplicationInfo()
		{
			this.Name = "";
			this.Path = "";
		}

        /// <summary>
        /// This object holds the basic information necessary for identifying an application.
        /// </summary>
        /// <param name="name">The friendly name of this application.</param>
        /// <param name="path">Path to the shortcut.</param>
        /// <param name="target">Path to the executable.</param>
        /// <param name="icon">ImageSource used to denote the application's icon in a graphical environment.</param>
        public ApplicationInfo(string name, string path, string target, ImageSource icon, string iconColor, string iconPath)
		{
			this.Name = name;
			this.Path = path;
            this.Target = target;
            this.Icon = icon;
            this.IconColor = iconColor;
            this.IconPath = iconPath;
        }

        private string name;
        /// <summary>
        /// The friendly name of this application.
        /// </summary>
        public string Name {
            get { return name; }
            set {
                name = value;
                // Notify Databindings of property change
                if (PropertyChanged != null) {
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
                }
            }
        }
        
        private string path;
		/// <summary>
        /// Path to the shortcut.
		/// </summary>
        public string Path {
            get { return path; }
            set {
                path = value;
                // Notify Databindings of property change
                if (PropertyChanged != null) {
                    PropertyChanged(this, new PropertyChangedEventArgs("Path"));
                }
            }
        }

        private string target;
        /// <summary>
        /// Path to the executable.
        /// </summary>
        public string Target
        {
            get { return target; }
            set
            {
                target = value;
                // Notify Databindings of property change
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Target"));
                }
            }
        }

        private string iconColor;
        /// <summary>
        /// Path to the executable.
        /// </summary>
        public string IconColor
        {
            get {
                if (!string.IsNullOrEmpty(iconColor))
                    return iconColor;
                else
                    return "Transparent";
            }
            set
            {
                iconColor = value;
                // Notify Databindings of property change
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IconColor"));
                }
            }
        }

        private string iconPath;
        /// <summary>
        /// Path to the executable.
        /// </summary>
        public string IconPath
        {
            get { return iconPath; }
            set
            {
                iconPath = value;
                // Notify Databindings of property change
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IconPath"));
                }
            }
        }

        private ImageSource icon;
        /// <summary>
        /// ImageSource used to denote the application's icon in a graphical environment.
        /// </summary>
        public ImageSource Icon {
            get
            {
                if (icon == null)
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {

                        icon = GetAssociatedIcon();
                        icon.Freeze();
                        // Notify Databindings of property change
                        if (PropertyChanged != null)
                        {
                            PropertyChanged(this, new PropertyChangedEventArgs("Icon"));
                        }
                    }));
                }

                return icon;
            }
            set {
                icon = value;
                // Notify Databindings of property change
                if (PropertyChanged != null) {
                    PropertyChanged(this, new PropertyChangedEventArgs("Icon"));
                }
            }
        }

        private Category category;
        /// <summary>
        /// The Category object to which this ApplicationInfo object belongs.
        /// Note: DO NOT ASSIGN MANUALLY. This property should only be set by a Category oject when adding/removing from its internal list.
        /// </summary>
        public Category Category {
            get { return category; }
            set {
                category = value;
                // Notify Databindings of property change
                if (PropertyChanged != null) {
                    PropertyChanged(this, new PropertyChangedEventArgs("Category"));
                }
            }
        }
		
        /// <summary>
        /// Determines if this ApplicationInfo object refers to the same application as another ApplicationInfo object.
        /// </summary>
        /// <param name="other">ApplicationInfo object to compare to.</param>
        /// <returns>True if the Name and Path values are equal, False if not.</returns>
		public bool Equals(ApplicationInfo other) {
            if (this.Name != other.Name) return false;
            if (this.Path == other.Path ) {
                return true;
            }
            if (System.IO.Path.GetExtension(this.Path).Equals(".lnk", StringComparison.OrdinalIgnoreCase)) {
                if (new Interop.Shell.Link(this.Path).Target == new Interop.Shell.Link(other.Path).Target) {
                    return true;
                }
            }
			return false;
		}
		
        /// <summary>
        /// Determines if this ApplicationInfo object refers to the same application as another ApplicationInfo object.
        /// </summary>
        /// <param name="obj">Object to compare to.</param>
        /// <returns>True if the Name and Path values are equal, False if not.</returns>
		public override bool Equals(object obj)
		{
			if (!(obj is ApplicationInfo)) return false;
			return this.Equals((ApplicationInfo) obj);
		}
		
		public override int GetHashCode()
		{
			int hashCode = 0;
			if (Name != null) hashCode ^= Name.GetHashCode();
			if (Path != null) hashCode ^= Path.GetHashCode();
			return hashCode;
		}
		
        /// <summary>
        /// Is this object greater than, less than, or equal to another ApplicationInfo? (For sorting purposes only)
        /// </summary>
        /// <param name="other">Object to compare to.</param>
        /// <returns>0 if same, negative if less, positive if more.</returns>
		public int CompareTo(ApplicationInfo other)
		{
			return this.Name.CompareTo(other.Name);
		}
		
		public override string ToString()
		{
			return string.Format("Name={0} Path={1}", this.Name, this.Path);
		}
		
		public static bool operator ==(ApplicationInfo x, ApplicationInfo y) {
			return x.Equals(y);
		}

		public static bool operator !=(ApplicationInfo x, ApplicationInfo y) {
			return !(x == y);
		}

        /// <summary>
        /// This Event is raised whenever a property of this object has changed. Necesary to sync state when binding.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets an ImageSource object representing the associated icon of a file.
        /// </summary>
        public ImageSource GetAssociatedIcon() {
            if (this.path.StartsWith("appx:"))
            {
                if (string.IsNullOrEmpty(this.IconPath) || !Interop.Shell.Exists(this.IconPath))
                {
                    try
                    {
                        this.IconPath = UWPInterop.StoreAppHelper.GetAppIcon(this.Target)[0];
                    }
                    catch
                    {
                        return IconImageConverter.GetDefaultIcon();
                    }
                }

                BitmapImage img = new BitmapImage();
                try
                {
                    img.BeginInit();
                    img.UriSource = new Uri(this.IconPath, UriKind.Absolute);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    img.Freeze();
                    return img;
                }
                catch
                {
                    return IconImageConverter.GetDefaultIcon();
                }
            }
            else if (this.path.StartsWith("appx:"))
                return IconImageConverter.GetDefaultIcon();
            else
                return IconImageConverter.GetImageFromAssociatedIcon(this.Path, true);
        }

        /// <summary>
        /// Create a copy of this object.
        /// </summary>
        /// <returns>A new ApplicationInfo object with the same data as this object, not bound to a Category.</returns>
        internal ApplicationInfo Clone() {
            ApplicationInfo rval = new ApplicationInfo(this.Name, this.Path, this.Target, this.Icon, this.IconColor, this.IconPath);
            return rval;
        }
    }

}
