using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileHelpers;

namespace MovieFiler
{
    [DelimitedRecord(",")]
    [IgnoreFirst(1)]
    public class Movie
    {
        [FieldQuoted()]
        public string Title;
        [FieldQuoted()]
        public string OriginalTitle ;
        [FieldQuoted()]
        public string ImdbID ;
        [FieldConverter(ConverterKind.Date,"yyyy/MM/dd hh:mm:ss tt")]
        public DateTime? ReleaseDate ;
        [FieldQuoted()]
        public string Genre ;
        public double Rating ;
        [FieldQuoted()]
        public string FileName ;
        [FieldQuoted()]
        public string OriginalFileName ;
        [FieldConverter(ConverterKind.Date, "yyyy/MM/dd hh:mm:ss tt")]
        public DateTime FileCreatedDate ;
        [FieldConverter(ConverterKind.Date, "yyyy/MM/dd hh:mm:ss tt")]
        public DateTime FileModifiedDate ;
        
        /// <summary>
        /// If Undo is not an empty string after reading file, undo actions
        /// </summary>
        [FieldQuoted()]
        public string Undo ;

    }
}
