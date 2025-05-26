// this file is temporary to make sure that the search works for SharePoint Embedded
// reason for this, is the current SDK does not implement the search IncludeHiddenContent property.

using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Infrastructure.Implementations.SPE.Models
{
    /// <summary>
    /// Class implementing IParsable for search requests
    /// </summary>
    public class SearchRequestBody : IParsable
    {
        /// <summary>
        /// The collection of search requests
        /// </summary>
        [JsonPropertyName("requests")]
        public List<SearchRequestItem> Requests { get; set; } = new();

        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        /// <returns>An instance of IParsable</returns>
        public static SearchRequestBody CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            return new SearchRequestBody();
        }

        /// <summary>
        /// Gets the deserialization information to deserialize the instance
        /// </summary>
        /// <returns>A dictionary of property names and deserialization callbacks</returns>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                ["requests"] = n => { Requests = n.GetCollectionOfObjectValues<SearchRequestItem>(SearchRequestItem.CreateFromDiscriminatorValue)?.ToList(); }
            };
        }

        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteCollectionOfObjectValues("requests", Requests);
        }
    }

    /// <summary>
    /// Individual search request
    /// </summary>
    public class SearchRequestItem : IParsable
    {
        /// <summary>
        /// The entity types to search
        /// </summary>
        [JsonPropertyName("entityTypes")]
        public List<string> EntityTypes { get; set; } = new();

        /// <summary>
        /// The search query
        /// </summary>
        [JsonPropertyName("query")]
        public SearchQuery Query { get; set; } = new();

        /// <summary>
        /// Maximum number of results
        /// </summary>
        [JsonPropertyName("size")]
        public int Size { get; set; }

        /// <summary>
        /// SharePoint and OneDrive specific search options
        /// </summary>
        [JsonPropertyName("sharePointOneDriveOptions")]
        public SharePointOneDriveOptions SharePointOneDriveOptions { get; set; } = new();

        /// <summary>
        /// The starting index for results
        /// </summary>
        [JsonPropertyName("from")]
        public int From { get; set; }

        /// <summary>
        /// Fields to retrieve 
        /// </summary>
        [JsonPropertyName("fields")]
        public List<string> Fields { get; set; } = new();

        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        /// <returns>An instance of IParsable</returns>
        public static SearchRequestItem CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            return new SearchRequestItem();
        }

        /// <summary>
        /// Gets the deserialization information to deserialize the instance
        /// </summary>
        /// <returns>A dictionary of property names and deserialization callbacks</returns>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                ["entityTypes"] = n => { EntityTypes = n.GetCollectionOfPrimitiveValues<string>()?.ToList(); },
                ["query"] = n => { Query = n.GetObjectValue<SearchQuery>(SearchQuery.CreateFromDiscriminatorValue); },
                ["size"] = n => { Size = n.GetIntValue() ?? 100; },
                ["sharePointOneDriveOptions"] = n => { SharePointOneDriveOptions = n.GetObjectValue<SharePointOneDriveOptions>(SharePointOneDriveOptions.CreateFromDiscriminatorValue); },
                ["from"] = n => { From = n.GetIntValue() ?? 0; },
                ["fields"] = n => { Fields = n.GetCollectionOfPrimitiveValues<string>()?.ToList(); }
            };
        }

        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteCollectionOfPrimitiveValues("entityTypes", EntityTypes);
            writer.WriteObjectValue("query", Query);
            writer.WriteIntValue("size", Size);
            writer.WriteObjectValue("sharePointOneDriveOptions", SharePointOneDriveOptions);
            writer.WriteIntValue("from", From);
            writer.WriteCollectionOfPrimitiveValues("fields", Fields);
        }
    }

    /// <summary>
    /// Query for search
    /// </summary>
    public class SearchQuery : IParsable
    {
        /// <summary>
        /// Query string
        /// </summary>
        [JsonPropertyName("queryString")]
        public string QueryString { get; set; }

        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        /// <returns>An instance of IParsable</returns>
        public static SearchQuery CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            return new SearchQuery();
        }

        /// <summary>
        /// Gets the deserialization information to deserialize the instance
        /// </summary>
        /// <returns>A dictionary of property names and deserialization callbacks</returns>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                ["queryString"] = n => { QueryString = n.GetStringValue(); }
            };
        }

        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("queryString", QueryString);
        }
    }

    /// <summary>
    /// SharePoint and OneDrive search options
    /// </summary>
    public class SharePointOneDriveOptions : IParsable
    {
        /// <summary>
        /// Include hidden content in search results
        /// </summary>
        [JsonPropertyName("includeHiddenContent")]
        public bool IncludeHiddenContent { get; set; }

        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        /// <returns>An instance of IParsable</returns>
        public static SharePointOneDriveOptions CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            return new SharePointOneDriveOptions();
        }

        /// <summary>
        /// Gets the deserialization information to deserialize the instance
        /// </summary>
        /// <returns>A dictionary of property names and deserialization callbacks</returns>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>>
            {
                ["includeHiddenContent"] = n => { IncludeHiddenContent = n.GetBoolValue() ?? false; }
            };
        }

        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteBoolValue("includeHiddenContent", IncludeHiddenContent);
        }
    }
}
