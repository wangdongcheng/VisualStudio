﻿using System;
using System.Collections.Generic;

namespace GitHub.Models
{
    /// <summary>
    /// Base class for issue and pull request detail models.
    /// </summary>
    public class IssueishDetailModel
    {
        /// <summary>
        /// Gets or sets the GraphQL ID of the issue or pull request.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the issue or pull request number.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the issue or pull request author.
        /// </summary>
        public ActorModel Author { get; set; }

        /// <summary>
        /// Gets or sets the issue or pull request title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the issue or pull request body.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the date/time at which the issue or pull request was last updated.
        /// </summary>
        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the comments on the issue or pull request.
        /// </summary>
        public IReadOnlyList<CommentModel> Comments { get; set; }

        /// <summary>
        /// Gets or sets the number of comments on the issue or pull request.
        /// </summary>
        public int CommentCount { get; set; }
    }
}
