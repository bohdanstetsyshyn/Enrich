CREATE TABLE "Role" (
    "id" SERIAL PRIMARY KEY,
    "role_name" VARCHAR(20) NOT NULL
);

CREATE TABLE "User" (
    "id" SERIAL PRIMARY KEY,
    "username" VARCHAR(50) NOT NULL,
    "email" VARCHAR(255) NOT NULL UNIQUE,
    "password_hash" VARCHAR(255) NOT NULL,
    "role_id" INT NOT NULL,
    "theme_preference" VARCHAR(10),
    "created_at" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "Category" (
    "id" SERIAL PRIMARY KEY,
    "name" VARCHAR(50) NOT NULL
);

CREATE TABLE "Tag" (
    "id" SERIAL PRIMARY KEY,
    "name" VARCHAR(50) NOT NULL
);

CREATE TABLE "Word" (
    "id" SERIAL PRIMARY KEY,
    "term" VARCHAR(100) NOT NULL,
    "definition" TEXT,
    "translation" VARCHAR(100),
    "difficulty_level" VARCHAR(5),
    "is_global" BOOLEAN NOT NULL DEFAULT FALSE,
    "creator_id" INT
);

CREATE TABLE "WordTag" (
    "word_id" INT NOT NULL,
    "tag_id" INT NOT NULL,
    PRIMARY KEY ("word_id", "tag_id")
);

CREATE TABLE "WordCategory" (
    "word_id" INT NOT NULL,
    "category_id" INT NOT NULL,
    PRIMARY KEY ("word_id", "category_id")
);

CREATE TABLE "Bundle" (
    "id" SERIAL PRIMARY KEY,
    "title" VARCHAR(150) NOT NULL,
    "description" TEXT,
    "owner_id" INT NOT NULL,
    "status" VARCHAR(20),
    "is_system" BOOLEAN NOT NULL DEFAULT FALSE,
    "admin_id" INT,
    "created_at" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "BundleWord" (
    "bundle_id" INT NOT NULL,
    "word_id" INT NOT NULL,
    PRIMARY KEY ("bundle_id", "word_id")
);

CREATE TABLE "WordProgress" (
    "id" SERIAL PRIMARY KEY,
    "user_id" INT NOT NULL,
    "word_id" INT NOT NULL,
    "status" VARCHAR(20),
    "success_rate" FLOAT,
    "last_reviewed" TIMESTAMP
);

CREATE TABLE "TrainingSession" (
    "id" SERIAL PRIMARY KEY,
    "user_id" INT NOT NULL,
    "bundle_id" INT,
    "score" INT,
    "training_type" VARCHAR(30),
    "start_time" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "SessionResult" (
    "id" SERIAL PRIMARY KEY,
    "session_id" INT NOT NULL,
    "word_id" INT NOT NULL,
    "is_correct" BOOLEAN NOT NULL
);

CREATE TABLE "CommunityImport" (
    "id" SERIAL PRIMARY KEY,
    "user_id" INT NOT NULL,
    "bundle_id" INT,
    "word_id" INT,
    "imported_at" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE "User" ADD CONSTRAINT "fk_user_role" FOREIGN KEY ("role_id") REFERENCES "Role"("id");

ALTER TABLE "Word" ADD CONSTRAINT "fk_word_creator" FOREIGN KEY ("creator_id") REFERENCES "User"("id");

ALTER TABLE "WordTag" ADD CONSTRAINT "fk_wordtag_word" FOREIGN KEY ("word_id") REFERENCES "Word"("id");
ALTER TABLE "WordTag" ADD CONSTRAINT "fk_wordtag_tag" FOREIGN KEY ("tag_id") REFERENCES "Tag"("id");

ALTER TABLE "WordCategory" ADD CONSTRAINT "fk_wordcategory_word" FOREIGN KEY ("word_id") REFERENCES "Word"("id");
ALTER TABLE "WordCategory" ADD CONSTRAINT "fk_wordcategory_category" FOREIGN KEY ("category_id") REFERENCES "Category"("id");

ALTER TABLE "Bundle" ADD CONSTRAINT "fk_bundle_owner" FOREIGN KEY ("owner_id") REFERENCES "User"("id");
ALTER TABLE "Bundle" ADD CONSTRAINT "fk_bundle_admin" FOREIGN KEY ("admin_id") REFERENCES "User"("id");

ALTER TABLE "BundleWord" ADD CONSTRAINT "fk_bundleword_bundle" FOREIGN KEY ("bundle_id") REFERENCES "Bundle"("id");
ALTER TABLE "BundleWord" ADD CONSTRAINT "fk_bundleword_word" FOREIGN KEY ("word_id") REFERENCES "Word"("id");

ALTER TABLE "WordProgress" ADD CONSTRAINT "fk_wordprogress_user" FOREIGN KEY ("user_id") REFERENCES "User"("id");
ALTER TABLE "WordProgress" ADD CONSTRAINT "fk_wordprogress_word" FOREIGN KEY ("word_id") REFERENCES "Word"("id");

ALTER TABLE "TrainingSession" ADD CONSTRAINT "fk_trainingsession_user" FOREIGN KEY ("user_id") REFERENCES "User"("id");
ALTER TABLE "TrainingSession" ADD CONSTRAINT "fk_trainingsession_bundle" FOREIGN KEY ("bundle_id") REFERENCES "Bundle"("id");

ALTER TABLE "SessionResult" ADD CONSTRAINT "fk_sessionresult_session" FOREIGN KEY ("session_id") REFERENCES "TrainingSession"("id");
ALTER TABLE "SessionResult" ADD CONSTRAINT "fk_sessionresult_word" FOREIGN KEY ("word_id") REFERENCES "Word"("id");

ALTER TABLE "CommunityImport" ADD CONSTRAINT "fk_communityimport_user" FOREIGN KEY ("user_id") REFERENCES "User"("id");
ALTER TABLE "CommunityImport" ADD CONSTRAINT "fk_communityimport_bundle" FOREIGN KEY ("bundle_id") REFERENCES "Bundle"("id");
ALTER TABLE "CommunityImport" ADD CONSTRAINT "fk_communityimport_word" FOREIGN KEY ("word_id") REFERENCES "Word"("id");