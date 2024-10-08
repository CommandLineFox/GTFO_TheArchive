﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TheArchive.Core.FeaturesAPI
{
    public static class FeatureGroups
    {
        /// <summary>
        /// Get a <see cref="Group"/> for the given string <paramref name="name"/>
        /// </summary>
        /// <param name="name">The name of the <see cref="Group"/> to get</param>
        /// <returns>An existing <see cref="Group"/> or <c>null</c> if it doesn't exist</returns>
        public static Group Get(string name) => Group.Get(name);

        /// <summary>
        /// Get or create a <see cref="Group"/> for the given string <paramref name="name"/>
        /// </summary>
        /// <param name="name">The name of the <see cref="Group"/> to get or create</param>
        /// <param name="groupModification">An <seealso cref="Action{Group}"/> that can be used to modify <seealso cref="Group"/> data.</param>
        /// <returns>An existing <see cref="Group"/> or a new one if it doesn't exist</returns>
        public static Group GetOrCreate(string name, Action<Group> groupModification = null) => Group.GetOrCreate(name, groupModification);

        // Group System kinda jank ngl haha

        public class Group
        {
            private static readonly HashSet<Group> _allGroups = new HashSet<Group>();

            public static Group Get(string name)
            {
                return _allGroups.FirstOrDefault(g => g.Name == name);
            }

            public string Name { get; private set; }
            public bool InlineSettings { get; internal set; }
            public bool IsNewlyCreated { get; private set; } = true;

            public static Group GetOrCreate(string name, Action<Group> groupModification = null)
            {
                var group = _allGroups.FirstOrDefault(g => g.Name == name);

                if (group != null)
                    group.IsNewlyCreated = false;

                group ??= new Group(name);

                groupModification?.Invoke(group);

                return group;
            }

            private Group(string name)
            {
                Name = name;

                _allGroups.Add(this);
            }

            public override string ToString()
            {
                return Name;
            }

            public static implicit operator string(Group g) => g.Name; 
        }

        public static Group Accessibility { get; private set; } = Group.GetOrCreate("Accessibility");
        internal static Group ArchiveCore { get; private set; } = Group.GetOrCreate("Archive Core");
        public static Group Audio { get; private set; } = Group.GetOrCreate("Audio");
        public static Group Backport { get; private set; } = Group.GetOrCreate("Backport");
        public static Group Cosmetic { get; private set; } = Group.GetOrCreate("Cosmetic");
        public static Group Dev { get; private set; } = Group.GetOrCreate("Developer");
        public static Group Fixes { get; private set; } = Group.GetOrCreate("Fixes");
        public static Group Hud { get; private set; } = Group.GetOrCreate("HUD / UI");
        public static Group LocalProgression { get; private set; } = Group.GetOrCreate("Local Progression"); // , group => group.InlineSettings = true
        public static Group Special { get; private set; } = Group.GetOrCreate("Misc");
        public static Group Presence { get; private set; } = Group.GetOrCreate("Discord / Steam Presence");
        public static Group Security { get; private set; } = Group.GetOrCreate("Security / Anti Cheat");
        public static Group QualityOfLife { get; private set; } = Group.GetOrCreate("Quality of Life");
    }
}
