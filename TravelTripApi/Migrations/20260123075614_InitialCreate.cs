using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace TravelTripApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "accounts_customuser",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    password = table.Column<string>(type: "text", nullable: false),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_superuser = table.Column<bool>(type: "boolean", nullable: false),
                    username = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    first_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    last_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    is_staff = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    date_joined = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts_customuser", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trips_trip",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    image = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    available_seats = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    duration = table.Column<int>(type: "integer", nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    keywords = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    embedding = table.Column<Vector>(type: "vector(768)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips_trip", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bookings_booking",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    paypal_transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    payment_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings_booking", x => x.id);
                    table.ForeignKey(
                        name: "FK_bookings_booking_accounts_customuser_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cart_cart",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_cart", x => x.id);
                    table.ForeignKey(
                        name: "FK_cart_cart_accounts_customuser_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chatbot_chathistory",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_message = table.Column<string>(type: "text", nullable: false),
                    ai_response = table.Column<string>(type: "text", nullable: false),
                    recommended_trip_ids = table.Column<string>(type: "text", nullable: true),
                    similarity_score = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chatbot_chathistory", x => x.id);
                    table.ForeignKey(
                        name: "FK_chatbot_chathistory_accounts_customuser_user_id",
                        column: x => x.user_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "bookings_bookingitem",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<int>(type: "integer", nullable: false),
                    trip_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings_bookingitem", x => x.id);
                    table.ForeignKey(
                        name: "FK_bookings_bookingitem_bookings_booking_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings_booking",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bookings_bookingitem_trips_trip_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips_trip",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cart_cartitem",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cart_id = table.Column<int>(type: "integer", nullable: false),
                    trip_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_cartitem", x => x.id);
                    table.ForeignKey(
                        name: "FK_cart_cartitem_cart_cart_cart_id",
                        column: x => x.cart_id,
                        principalTable: "cart_cart",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cart_cartitem_trips_trip_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips_trip",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_customuser_email",
                table: "accounts_customuser",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_booking_user_id",
                table: "bookings_booking",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_bookingitem_booking_id",
                table: "bookings_bookingitem",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_bookingitem_trip_id",
                table: "bookings_bookingitem",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_cart_user_id",
                table: "cart_cart",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_cartitem_cart_id",
                table: "cart_cartitem",
                column: "cart_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_cartitem_trip_id",
                table: "cart_cartitem",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "IX_chatbot_chathistory_session_id",
                table: "chatbot_chathistory",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_chatbot_chathistory_user_id",
                table: "chatbot_chathistory",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings_bookingitem");

            migrationBuilder.DropTable(
                name: "cart_cartitem");

            migrationBuilder.DropTable(
                name: "chatbot_chathistory");

            migrationBuilder.DropTable(
                name: "bookings_booking");

            migrationBuilder.DropTable(
                name: "cart_cart");

            migrationBuilder.DropTable(
                name: "trips_trip");

            migrationBuilder.DropTable(
                name: "accounts_customuser");
        }
    }
}
