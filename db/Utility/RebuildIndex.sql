-- Output script to rebuild all indexes

SELECT 'ALTER INDEX ALL ON ' + TABLE_SCHEMA + '.' + table_name + '  REBUILD;'
FROM Information_Schema.tables
WHERE table_type = 'BASE TABLE';