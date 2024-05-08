using System;
using System.Collections.ObjectModel;
using R3;

namespace Module
{
    public interface ITaggable
    {
        ReadOnlyCollection<string> Tags { get; }
        bool HasTag(string tag);
        //bool HasAll(params string[] tags);
        //bool HasAny(params string[] tags);
        
        /// <summary>
        /// returns true when the tag was added (did not already exist in collection)
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        bool AddTag(string tag);

        /// <summary>
        /// Returns true when the tag was removed (already existed in collection)
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        bool RemoveTag(string tag);
    }
}