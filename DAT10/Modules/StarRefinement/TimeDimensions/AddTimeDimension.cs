using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;
using DAT10.StarModelComponents.Dimensions;

namespace DAT10.Modules.StarRefinement.TimeDimensions
{
    public class AddTimeDimension : IStarRefinement
    {
        public string Name { get; } = "Add Date and Time Dimensions";
        public string Description { get; } = "Add a time dimension table and a date dimension table to a star model, if it does not already exist.";

        public StarModel Refine(StarModel starModel)
        {
            var factTable = starModel.FactTable;

            // If any date/time dimension already exists
            if (starModel.Dimensions.Any(d => d.Name.Contains("time") || d.Name.Contains("date")))
                return starModel;

            // Create date and time dimension for model. These are also used for roleplaying
            DateDimension actualDateDimension = CreateDateDimension();
            TimeDimension actualTimeDimension = CreateTimeDimension();

            // These can either be the actual dimension or roleplaying dimensions
            DateDimension dateDimension = null;
            TimeDimension timeDimension = null;

            // Add a date and/or time dimension to all columns that can express date or time
            foreach (var temporalColumn in factTable.Columns.Where(c => c.DataType.IsTemporal()).ToList())
            {
                // If date, only add date dimension, likewise with time. If a date/time dimension is already in use,
                // then use that for roleplaying
                switch (temporalColumn.DataType.Type)
                {
                    case OleDbType.DBTime:
                        timeDimension = timeDimension != null ? new TimeDimension(actualTimeDimension) : actualTimeDimension;
                        break;
                    case OleDbType.Date:
                    case OleDbType.DBDate:
                        dateDimension = dateDimension != null ? new DateDimension(actualDateDimension) : actualDateDimension;
                        break;
                    case OleDbType.DBTimeStamp:
                        dateDimension = dateDimension != null ? new DateDimension(actualDateDimension) : actualDateDimension;
                        timeDimension = timeDimension != null ? new TimeDimension(actualTimeDimension) : actualTimeDimension;
                        break;
                }

                // If time dimension is set, then add it to the model and add a relation
                if (timeDimension != null)
                {
                    starModel.Dimensions.Add(timeDimension);

                    var timeForeignKey = new StarColumn(factTable.Columns.Count + 1, $"{temporalColumn.Name}_Time_Key", new DataType(OleDbType.Integer), StarColumnType.Key);
                    timeForeignKey.ColumnRef = temporalColumn.ColumnRef;

                    var relation = new StarRelation(timeDimension, factTable, new List<StarColumn> { timeDimension.Columns[0] }, new List<StarColumn> { timeForeignKey }, Cardinality.ManyToOne);
                    factTable.Relations.Add(relation);
                    timeDimension.Relations.Add(relation);

                    factTable.Columns.Add(timeForeignKey);
                    factTable.Constraints.PrimaryKey.Columns.Add(timeForeignKey);
                }

                // If date dimension is set, then add it to the model and add a relation
                if (dateDimension != null)
                {
                    starModel.Dimensions.Add(dateDimension);

                    var dateForeignKey = new StarColumn(factTable.Columns.Count + 1, $"{temporalColumn.Name}_Date_Key", new DataType(OleDbType.Integer), StarColumnType.Key);
                    dateForeignKey.ColumnRef = temporalColumn.ColumnRef;

                    var relation = new StarRelation(dateDimension, factTable, new List<StarColumn> { dateDimension.Columns[0] }, new List<StarColumn> { dateForeignKey }, Cardinality.ManyToOne);
                    factTable.Relations.Add(relation);
                    dateDimension.Relations.Add(relation);

                    factTable.Columns.Add(dateForeignKey);
                    factTable.Constraints.PrimaryKey.Columns.Add(dateForeignKey);
                }

                temporalColumn.ColumnType = StarColumnType.DescriptiveMeasure;
                
                // Cleanup 
                factTable.Columns.Remove(temporalColumn);
            }

            return starModel;
        }

        private static DateDimension CreateDateDimension()
        {
            var dateDimension = new DateDimension("Date", DateDimension.DateGranularity.Days);
            var primaryColumn = new StarColumn(1, "SurKey", new DataType(OleDbType.Integer), StarColumnType.Key | StarColumnType.SurrogateKey);
            dateDimension.Constraints.PrimaryKey.Columns.Add(primaryColumn);
            dateDimension.Constraints.NotNullables.Add(new StarModelComponents.NotNullable(primaryColumn));

            var fullColumn   = new StarColumn(2, "FullDate", new DataType(OleDbType.WChar, 10), StarColumnType.DescriptiveMeasure);
            var dayColumn    = new StarColumn(3, "Day", new DataType(OleDbType.SmallInt), StarColumnType.DescriptiveMeasure);
            var nDayColumn   = new StarColumn(4, "NameOfDay", new DataType(OleDbType.WChar, 10), StarColumnType.DescriptiveMeasure);
            var weekColumn   = new StarColumn(5, "Week", new DataType(OleDbType.SmallInt), StarColumnType.DescriptiveMeasure);
            var monthColumn  = new StarColumn(6, "Month", new DataType(OleDbType.SmallInt), StarColumnType.DescriptiveMeasure);
            var nMonthColumn = new StarColumn(7, "NameOfMonth", new DataType(OleDbType.WChar, 10), StarColumnType.DescriptiveMeasure);
            var yearColumn   = new StarColumn(8, "Year", new DataType(OleDbType.SmallInt), StarColumnType.DescriptiveMeasure);

            var isHolidayColumn = new StarColumn(9, "IsHoliday", new DataType(OleDbType.Boolean), StarColumnType.DescriptiveMeasure);
            var holidayColumn   = new StarColumn(10, "Holiday", new DataType(OleDbType.WChar, 32), StarColumnType.DescriptiveMeasure);

            dateDimension.Columns.Add(primaryColumn);   // SurKey
            dateDimension.Columns.Add(fullColumn);      // FullDate

            dateDimension.Columns.Add(dayColumn);       // Day
            dateDimension.Columns.Add(nDayColumn);      // NameOfDay

            dateDimension.Columns.Add(weekColumn);      // Week

            dateDimension.Columns.Add(monthColumn);     // Month
            dateDimension.Columns.Add(nMonthColumn);    // NameOfMonth

            dateDimension.Columns.Add(yearColumn);      // Year

            dateDimension.Columns.Add(isHolidayColumn); // IsHoliday
            dateDimension.Columns.Add(holidayColumn);   // Holiday

            primaryColumn.TableRef = dateDimension;
            fullColumn.TableRef = dateDimension;
            dayColumn.TableRef = dateDimension;
            nDayColumn.TableRef = dateDimension;
            weekColumn.TableRef = dateDimension;
            monthColumn.TableRef = dateDimension;
            nMonthColumn.TableRef = dateDimension;
            yearColumn.TableRef = dateDimension;
            isHolidayColumn.TableRef = dateDimension;
            holidayColumn.TableRef = dateDimension;

            return dateDimension;
        }

        private static TimeDimension CreateTimeDimension()
        {
            var timeDimension = new TimeDimension("Time", TimeDimension.TimeGranularity.Seconds);
            var primaryColumn = new StarColumn(1, "SurKey", new DataType(OleDbType.Integer), StarColumnType.Key | StarColumnType.SurrogateKey);
            timeDimension.Constraints.PrimaryKey.Columns.Add(primaryColumn);
            timeDimension.Constraints.NotNullables.Add(new StarModelComponents.NotNullable(primaryColumn));

            var hourColumn   = new StarColumn(2, "Hour", new DataType(OleDbType.SmallInt), StarColumnType.DescriptiveMeasure);
            var minuteColumn = new StarColumn(3, "Minute", new DataType(OleDbType.SmallInt), StarColumnType.DescriptiveMeasure);
            var secondColumn = new StarColumn(4, "Second", new DataType(OleDbType.SmallInt), StarColumnType.DescriptiveMeasure);

            timeDimension.Columns.Add(primaryColumn); // Surkey
            timeDimension.Columns.Add(hourColumn);    // Hour
            timeDimension.Columns.Add(minuteColumn);  // Minute
            timeDimension.Columns.Add(secondColumn);  // Second

            primaryColumn.TableRef = timeDimension;
            hourColumn.TableRef = timeDimension;
            minuteColumn.TableRef = timeDimension;
            secondColumn.TableRef = timeDimension;

            return timeDimension;
        }
    }
}
