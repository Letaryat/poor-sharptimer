ALTER TABLE "PlayerRecords" ADD COLUMN "Mode" VARCHAR(24) DEFAULT '';
ALTER TABLE "PlayerRecords" DROP CONSTRAINT IF EXISTS "PlayerRecords_pkey";
ALTER TABLE "PlayerRecords" DROP CONSTRAINT IF EXISTS "pk_Records";
ALTER TABLE "PlayerRecords" ADD CONSTRAINT pk_Records PRIMARY KEY ("MapName", "SteamID", "Style", "Mode");
ALTER TABLE "PlayerStats" ADD COLUMN "Mode" VARCHAR(24) DEFAULT 'None';