Catel.Fody
==========

[![Join the chat at https://gitter.im/Catel/Catel.Fody](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/Catel/Catel.Fody?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

![License](https://img.shields.io/github/license/catel/catel.fody.svg)
![NuGet downloads](https://img.shields.io/nuget/dt/catel.fody.svg)
![Version](https://img.shields.io/nuget/v/catel.fody.svg)
![Pre-release version](https://img.shields.io/nuget/vpre/catel.fody.svg)

Catel.Fody is an addin for Fody (see https://github.com/Fody/Fody), which
is an extensible tool for weaving .net assemblies. 

This addin will rewrite simple properties to the dependency-property alike 
properties that are used inside Catel.

It will rewrite all properties on the DataObjectBase and ViewModelBase. So, a
property that is written as this:

    public string FirstName { get; set; }

will be weaved into

    public string FirstName
    {
        get { return GetValue<string>(FirstNameProperty); }
        set { SetValue(FirstNameProperty, value); }
    }

    public static readonly PropertyData FirstNameProperty = RegisterProperty("FirstName", typeof(string));

but if a readonly computed property like this one exists:

    public string FullName
    {
        get { return string.Format("{0} {1}", FirstName, LastName).Trim(); }
    }

the *OnPropertyChanged* method will be also weaved into

	protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (e.PropertyName.Equals("FirstName"))
		{
			base.RaisePropertyChanged("FullName");
		}

		if (e.PropertyName.Equals("LastName"))
		{
			base.RaisePropertyChanged("FullName");
		}
	}


## Documentation

Documentation can be found at http://www.catelproject.com

## Issue tracking

The issue tracker including a roadmap can be found at http://www.catelproject.com
