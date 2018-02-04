using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable 0168

namespace BaseLib
{
    public interface IPropertilized
    {
        IProperty GetProperty(string strName);
        void AddProperty(string strName, IProperty pProperty);
        bool HasProperty(string strName);
        void RemoveAllPropertys();
    }
    
    public class PropertyContainer
    {
        public Func<string, IProperty> OnCreateProperty;
    
	    private Dictionary<string, IProperty> _properties;
	
	    public PropertyContainer()
	    {
		    _properties = new Dictionary<string, IProperty>();
	    }

        public T GetProperty<T>(string strName) where T : class, IProperty
        {
            var prop = GetProperty(strName);
            Debugger.Assert(prop is T);
            return prop as T;
        }

        public IProperty GetProperty(string strName)
	    {
            IProperty property = null;

            try
            {
		        property = _properties[strName];
            }		    
		    catch(Exception e)
		    {
                Debugger.Assert(OnCreateProperty != null);
                property = OnCreateProperty(strName);
			
			    if (property == null)
			    {
                    Debugger.Assert(false);
				    ///...property = new RProperty(this, strName);				
			    }
		    }		
		    return property;
	    }

        public void AddProperty(string strName, IProperty pProperty)
	    {
		    Debugger.Assert(!HasProperty(strName));
		    _properties.Add(strName, pProperty);
	    }

        public bool HasProperty(string strName)
	    {
		    return _properties.ContainsKey(strName);
	    }
	
	    public void RemoveAllPropertys()
	    {
            _properties.Clear();
	    }
    }


}



