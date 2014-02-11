using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VoterDB
{
    // Application specific stuff.
    public partial class Client
    {
        readonly string _prefix = "http://admin.targetedvrm.us/api/";
        readonly string _token;
        readonly string _apiKey;

        readonly HttpClient _client;
        
        public Client(string token, string apiKey)
        {
            _token = token;
            _apiKey = apiKey;

            _client = new HttpClient();
        }

        private HttpRequestMessage MakeRequest(HttpMethod method, string url, params object[] args)
        {
            string url2 = string.Format(url, args);

            HttpRequestMessage req = new HttpRequestMessage(method, _prefix + url2);
            req.Headers.Add("x-vrm-token", _token);
            req.Headers.Add("x-vrm-appkey", _apiKey);

            return req;

        }

        public User GetCurrentUser()
        {
            var req = MakeRequest(HttpMethod.Get, "users/current");
            var user = Send<User>(req);
            return user;
        }

        public IEnumerable<Bookmark> GetBookmarks()
        {
            var req = MakeRequest(HttpMethod.Get, "bookmarks");
            var bookmarks= Send<Bookmark[]>(req);
            return bookmarks;
        }

        public Bookmark GetBookmark(string name)
        {
            var req = MakeRequest(HttpMethod.Get, "bookmarks");
            var bookmarks = Send<Bookmark[]>(req);

            return (from x in bookmarks where IsEqual(x.name, name) select x).First();
        }

        // Get an individual contact, which includes rich details from all their fields.
        public ContactDetails GetContactDetails(Contact contact)
        {
            return GetContactDetails(contact.con_id);
        }

        public ContactDetails GetContactDetails(ContactId contactId)
        {
            // string X = File.ReadAllText(@"c:\temp\data.txt");

            var req = MakeRequest(HttpMethod.Get, "contacts/{0}", contactId);
            string json = SendRawJson(req);

            var y = ContactDetails.Parse(json);
            Verify(y);
            return y.data;
        }        

        // GEt contacts in this bookmark
        public IEnumerable<Contact> GetContacts(Bookmark bookmark)
        {
            string q = string.Format("contacts?bookmark={0}", bookmark.id);

            return GetContactsWorker(q);
        }
                
        // Get all contacts
        public IEnumerable<Contact> GetContacts()
        {
            return GetContactsWorker("contacts");
        }

        // GET http://admin.targetedvrm.us/api/contacts
        // Page through, via ?page=
        private IEnumerable<Contact> GetContactsWorker(string url)
        {
            return GetPagedData<Contact>(url);            
        }

        public IEnumerable<Note> GetNotes(Contact contact)
        {
            return GetNotes(contact.con_id);
        }

        // GET http://admin.targetedvrm.us/api/notes?contact=526a904d399cce2532f581c4
        // $$$ Page through this. 
        public IEnumerable<Note> GetNotes(ContactId contactId)
        {
            var results = GetPagedData<Note>("notes?contact={0}", contactId);            
            return results;
        }

        // POST http://admin.targetedvrm.us/api/notes 
        public Note CreateNote(string newContent, string contactId)
        {
            var body = new NoteUpdate
            {
                 contact_id = contactId,
                 body = newContent
            };
            var request = MakeRequest(HttpMethod.Post, "notes");
            AddBody(request, body);
            
            var res = Send<NoteUpdateResult>(request);

            // $$$ leaving a lot empty here so we can reuse an existing typeB, but it has the essentials.
            return new Note
            {
                 body = newContent,
                 id = res.id,
                 contact_id = contactId                    
            };
        }

        // PUT http://admin.targetedvrm.us/api/notes/7
        public void UpdateNote(Note note, string newContent)
        {
            var id = note.id;
            NoteUpdate n = new NoteUpdate
            {
                 body = newContent,
                 contact_id =  note.contact_id                 
            };

            var request = MakeRequest(HttpMethod.Put, "notes/{0}", id);
            AddBody(request, n);
            var res = Send<NoteUpdateResult>(request);

            if (res.id != id)
            {
                // Mismatch!
                throw new InvalidOperationException("Note updated failed. Id mismatch.");
            }
        }

        public void DeleteNote(Note note)
        {
            DeleteNote(note.id);
        }

        public void DeleteNote(NoteId id)
        {
            var request = MakeRequest(HttpMethod.Delete, "notes/{0}", id);
            
            var res = Send<NoteUpdateResult>(request);

            if (res.id != id)
            {
                // Mismatch!
                throw new InvalidOperationException("Note delete failed. Id mismatch.");
            }
        }

        // GET http://admin.targetedvrm.us/api/buckets
        public IEnumerable<Bucket> GetBuckets()
        {
            var req = MakeRequest(HttpMethod.Get, "buckets");

            var results = Send<Bucket[]>(req);

            return results;
        }

        // Lookup bucket Id by name
        public BucketId GetBucketIdByName(string name)
        {
            var buckets = GetBuckets();
            return (from x in buckets where IsEqual(x.title, name) select x).First().id;
        }

        static bool IsEqual(string a, string b)
        {
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase) == 0;
        }

        // Beware, you can have multiple buckets with the same name and different ids. 
        public BucketId CreateBucket(string name)
        {
            var req = MakeRequest(HttpMethod.Post, "buckets");
            AddBody(req, new { title = name, type = "field" });

            var bucket = Send<Bucket>(req);
            return bucket.id;
        }

        // Deletes a bucket. 
        // Fails if the bucket id is invalid (such as if it's already deleted). 
        public void DeleteBucket(BucketId id)
        {
            var req = MakeRequest(HttpMethod.Delete, "buckets/{0}", id);            
            Send(req);            
        }
                
        // Get all field descriptors, grouped by bucket
        public IEnumerable<FieldDescriptorGroup> GetFieldDescriptors()
        {
            //var json = "{}";
            //var obj = JsonConvert.DeserializeObject<IDictionary<string, int>>(json);

            var req = MakeRequest(HttpMethod.Get, "fields");
            var results = Send<FieldDescriptorGroup[]>(req);
            return results;            
        }

        public FieldId AddField(BucketId id, string fieldName, string label, FieldType type)
        {
            var x = new NewFieldDescriptor
            {
                bucket_id = id,
                label = label,
                name = fieldName,
                type = type
            };
            var req = MakeRequest(HttpMethod.Post, "fields");
            AddBody(req, x);

            var result = Send<NewFieldDescriptorResult>(req);
            return result.id;
        }

        // $$$ This could be multi-set on a single contact 
        public void SetField(ContactId id, BucketId bucketId, string fieldName, object value)
        {
            var req = MakeRequest(HttpMethod.Put, "contacts/{0}", id);
            
            IDictionary<BucketId, IDictionary<string, string>> vals = new 
            Dictionary<BucketId, IDictionary<string, string>>();
            vals[bucketId] = new Dictionary<string,string> { { fieldName, value.ToString() }};
            
            AddBody(req, vals);

            Send(req);
        }

        // PUT http://admin.targetedvrm.us/api/buckets/272 
        public void UpdateBucketTitle(BucketId id, string title)
        {
            var request = MakeRequest(HttpMethod.Put, "buckets/{0}", id);
            AddBody(request, new BucketTitle { title = title } );

            var res = Send<Bucket>(request);
        }
    }
}
