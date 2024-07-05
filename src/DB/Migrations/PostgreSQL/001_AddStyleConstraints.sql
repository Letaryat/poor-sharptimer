ALTER TABLE "PlayerRecords" DROP CONSTRAINT "pk_Records";
ALTER TABLE "PlayerRecords" ADD CONSTRAINT "pk_Records" PRIMARY KEY ("MapName", "SteamID", "Style");