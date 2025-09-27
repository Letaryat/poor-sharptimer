CREATE TABLE PlayerRecords_new (
    MapName TEXT,
    SteamID TEXT,
    PlayerName TEXT,
    TimerTicks INT,
    FormattedTime TEXT,
    UnixStamp INT,
    TimesFinished INT,
    LastFinished INT,
    Style INT,
    Mode TEXT DEFAULT '',
    PRIMARY KEY (MapName, SteamID, Style, Mode)
);

INSERT INTO PlayerRecords_new
SELECT MapName, SteamID, PlayerName, TimerTicks, FormattedTime, UnixStamp,
       TimesFinished, LastFinished, Style, ''
FROM PlayerRecords;

DROP TABLE PlayerRecords;
ALTER TABLE PlayerRecords_new RENAME TO PlayerRecords;

ALTER TABLE PlayerStats ADD COLUMN Mode TEXT DEFAULT 'None';