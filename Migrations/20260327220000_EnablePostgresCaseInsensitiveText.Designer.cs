using MangoTaika.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MangoTaika.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260327220000_EnablePostgresCaseInsensitiveText")]
    partial class EnablePostgresCaseInsensitiveText
    {
    }
}
