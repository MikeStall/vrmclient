using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoterDB
{
    [DebuggerDisplay("{Lat},{Long}")]
    public class Geo
    {
        readonly double[] _store;
        internal Geo(double[] store)
        {
            _store = store;
        }
        public double Lat
        {
            get { return _store[0]; }
        }
        public double Long
        {
            get { return _store[1]; }
        }
    }

    // Type safe wrappers over integer values.
    public enum BucketId
    {
    }

    public enum UserId
    {
    }

    public enum OrgId
    {
    }

    public enum FieldId
    {
    }

    public enum NoteId
    {
    }

    // Helper for strong-typing strings. 
    [JsonConverter(typeof(StringWrapperConverter))]
    public class BaseStringId
    {
        public string Value;

        public override string ToString()
        {
            return this.Value;
        }
        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            BaseStringId x = obj as BaseStringId;
            if (x != null)
            {
                return this.Value == x.Value;
            }
            return false;
        }
    }

    public class StringWrapperConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {   
            BaseStringId id = (BaseStringId) value;
            writer.WriteValue(id.Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var id = (string)reader.Value;
            var obj = Activator.CreateInstance(objectType); // Create derived type
            BaseStringId b = (BaseStringId)obj;
            b.Value = id;

            return obj;
        }

        public override bool CanConvert(Type objectType)
        {
            // Not implemented in BCL 
            //return typeof(BaseStringId).IsAssignableFrom(objectType);
            throw new NotImplementedException();
        }
    }

    public class BookmarkId : BaseStringId
    {
    }
    public class ContactId : BaseStringId
    {
    }

}