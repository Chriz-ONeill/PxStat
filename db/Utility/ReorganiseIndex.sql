-- Output script to reorganise all indexes
SELECT 'ALTER INDEX ALL ON ' + TABLE_SCHEMA + '.' + table_name + '  REORGANIZE;'
FROM Information_Schema.tables
WHERE table_type = 'BASE TABLE';