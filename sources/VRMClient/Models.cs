using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoterDB
{
    // This is the raw wire protocol from JSON serialization.

    // All results are wrapped in a top-level package that includes a success code. 
    // GET http://admin.targetedvrm.us/api/buckets
    public class Results
    {
        public string status { get; set; }
        public string message { get; set; }

        [JsonIgnore]
        public bool IsValid
        {
            get { return status == "ok"; }
        }
    }

    public class Results<TData> : Results
    {
        public TData data { get; set; }        
    }

    // For paged data
    public class Pages<TData>
    {
        public int count { get; set; }
        public int page { get; set; }
        public int pages { get; set; }

        public TData[] results { get; set; }
    }

    public class Note
    {
        public NoteId id { get; set; }
        public string contact_id { get; set; }
        public string body { get; set; }
        public OrgId org_id { get; set; }
        public UserId user_id { get; set; }
        public string contact_name { get; set; }
        public string user_name { get; set; }
    }

    // Body included with a PUT request
    // PUT http://admin.targetedvrm.us/api/notes/7
    class NoteUpdate
    {
        public string contact_id { get; set; }
        public string body { get; set; } // New content
        public int done { get; set; } // $$$ what is this ???
        public string date { get; set; }
    }

    // result returned from various node operations
    class NoteUpdateResult
    {
        public NoteId id { get; set; }
    }

    // GET http://admin.targetedvrm.us/api/contacts    
    // Page contents
    public class Contact
    {
        [JsonProperty("$loc")]
        private double[] _location { get; set; } // GEO coordinates
        
        [JsonIgnore] // Typesafe wrapper on _location
        public Geo loc {
            get 
            {
                return new Geo(_location);
            }
        }

        public string address { get; set; } // Street address
        public string city { get; set; }
        public string zip { get; set; } // 5 digit zip code
        public string zip4 { get; set; }
        
        public string county { get; set; }

        [JsonProperty("precinct number")]
        public int precinctnumber { get; set; }

        public int cd { get; set; } // Congressional district
        public int ld { get; set; } // legislative district

        public int age { get; set; }
        public DateTime dob { get; set; } // $$$ Make into DateTime

        public ContactId con_id { get; set; } // Contact ID. This is a string

        public string gender { get; set; } // $$$ Make typesafe
        public string name { get; set; }

        [JsonProperty("state voter id")]
        public string voterid { get; set; }
    }

    // http://admin.targetedvrm.us/api/contacts/526a904a399cce2532f466c7
    public class ContactDetails
    {
        [JsonProperty("$loc")]
        public double[] _location { get; set; } // GEO coordinates

        [JsonIgnore] // Typesafe wrapper on _location
        public Geo loc
        {
            get
            {
                return new Geo(_location);
            }
        }

        public string con_id { get; set; } // Contact ID. This is a string
        public string con_pid { get; set; }
        
        public string name { get; set; } // full name

        // This requires some dynamic deserialization because the raw JSON object
        // has fields with name BucketId of type FieldGroup
        public IDictionary<BucketId, FieldGroup> AllFieldsByBucket { get; set; }

        public static Results<ContactDetails> Parse(string json)
        {
            var o = JObject.Parse(json);

            // Parse the static stuff. This will skip the dynamic fields.
            var res = o.ToObject<Results<ContactDetails>>();
            res.data.AllFieldsByBucket = new Dictionary<BucketId, FieldGroup>();
            
            var o2 = o["data"];
            foreach (JProperty o3 in o2.Children())
            {
                var name = o3.Name;
                int id;
                if (int.TryParse(name, out id))
                {
                    var fg = o3.Value.ToObject<FieldGroup>();
                    res.data.AllFieldsByBucket[(BucketId) id] = fg;
                }
            }

            return res;
        }

        public IEnumerable<Field> GetAllFields()
        {
            foreach (var kv in this.AllFieldsByBucket)
            {
                foreach (var field in kv.Value.fields)
                {
                    yield return field.Value;
                }
            }
        }
    }

    // Group of fields within a bucket
    public class FieldGroup
    {
        public IDictionary<string, Field> fields { get; set; } // key is Field.name

        public string title { get; set; } // Name of the bucket
        public string type { get; set; } // "default", "field"
    }

    // $$$ This is really "Field w/ Value" as opposed to "Field Descriptor"
    [DebuggerDisplay("{name}={value}")]
    public class Field
    {
        public string value { get; set; }
        public FieldType type { get; set; } // "int", "email", "date", "string"
        public FieldId id { get; set; }

        public string name { get; set; }
        public string label { get; set; } // like name, but more friendly 
    }

    // Get all fields, grouped by bucket.
    // GET http://admin.targetedvrm.us/api/fields 
    // retruns an array of FieldDescriptors
    public class FieldDescriptorGroup
    {
        public string title { get; set; }
        public BucketId id { get; set; }

        // key is field name. 
        // $$$ BUG! Their server returns [] if fields is empty, which means an array 
        //public IDictionary<string, FieldDescriptor> fields { get; set; }
        public JToken fields { get; set; }

        public IDictionary<string, FieldDescriptor> ParseFields()
        {
            JArray array = fields as JArray;
            if (array != null)
            {
                // Server has a bug. Returns an [] if the dictionary has 0 elements. 
                return new Dictionary<string, FieldDescriptor>();
            }
            JObject obj = fields as JObject;
            var d2 = obj.ToObject<IDictionary<string, FieldDescriptor>>();

            return d2;
        }
    }

    // $$$ Is this the same as Field? I notice it has a value, but maybe that's "allowed values"    
    public class FieldDescriptor
    {
        public FieldId id { get; set; }
        public FieldType type { get; set; }
        public string name { get; set; }
        public string label { get; set; }
    }

    public class NewFieldDescriptor
    {
        public BucketId bucket_id { get; set; } // bucket this field will go in
        public FieldType type { get; set; }
        public string name { get; set; }
        public string label { get; set; }
    }

    public class NewFieldDescriptorResult
    {
        public FieldId id { get; set; } 
    }
    
    // GET http://admin.targetedvrm.us/api/users/current
    public class User
    {
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public UserId id { get; set; } 
        public OrgId org_id { get; set; } // $$$ Is this an int? What is this?

        public string mobile { get; set; }
        public string pn_sub_key { get; set; }
        public string group_type { get; set; }
        public bool isAdmin { get; set; }
    }

    // Buckets are like field namespaces for the contact data.
    [DebuggerDisplay("{title} ({id})")]
    public class Bucket
    {
        public BucketId id { get; set; }
        public string title { get; set; }
    }
        
    public class BucketTitle 
    {
        public string title { get; set; }
    }


    [DebuggerDisplay("{name}")]
    public class Bookmark
    {
        public string name { get; set; } // name of the bookmark

        public UserId user_id { get; set; }        
        
        // filters - this is a rich tree data structure for the filter. 
                
        public BookmarkId id { get; set; }
    }

    // Serialize as string 
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FieldType
    {
        @int,
        email,
        date,
        @bool,
        @string
    }
}
