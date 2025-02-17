﻿using System;
using System.Collections.Generic;

namespace GrowGreenWeb.Models
{
    public partial class Quiz
    {
        public Quiz()
        {
            QuizQuestions = new HashSet<QuizQuestion>();
        }

        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public int? LectureId { get; set; }
        public int CourseId { get; set; }

        public virtual Lecture? Lecture { get; set; }
        public virtual ICollection<QuizQuestion> QuizQuestions { get; set; }
    }
}
