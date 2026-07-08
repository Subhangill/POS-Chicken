// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


$(document).ready(function () {
    $('.datatable').each(function () {
        var $table = $(this);

        // Destroy if already initialized
        if ($.fn.DataTable.isDataTable(this)) {
            $table.DataTable().destroy();
        }



        var table = $table.DataTable({
            destroy: true,
            ordering: true,
            order: [[1, 'desc']],           // Default sort on 2nd column (Date usually)
            searching: true,
            info: true,
            lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "All"]],

            // Important settings for sorting
            orderCellsTop: false,           // Set to false if you have only one header row
            columnDefs: [
                { orderable: false, targets: -1 }        // Action column
            ],

            dom: "<'row mb-2'<'col-sm-12 d-flex align-items-center justify-content-between flex-wrap gap-3'<'d-flex align-items-center flex-wrap gap-3'Bl>>>" +
                "<'row'<'col-sm-12'tr>>" +
                "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",

            buttons: [
                { extend: 'copy', className: 'btn btn-secondary btn-sm' },
                { extend: 'csv', className: 'btn btn-secondary btn-sm' },
                { extend: 'excel', className: 'btn btn-secondary btn-sm' },
                { extend: 'pdf', className: 'btn btn-secondary btn-sm' },
                { extend: 'print', className: 'btn btn-secondary btn-sm' }
            ],

            language: {
                search: '',
                searchPlaceholder: "Search...",
                sLengthMenu: 'Show _MENU_ Entries',
                info: "Showing _START_ - _END_ of _TOTAL_ entries",
                paginate: {
                    next: '<i class="ti ti-chevron-right"></i>',
                    previous: '<i class="ti ti-chevron-left"></i>'
                }
            }
        });

        // Manual column search
        $table.find('thead input.input-dt-search').off('keyup change').on('keyup change', function () {
            var colIdx = $(this).closest('th').index();
            table.column(colIdx).search(this.value).draw();
        });
    });



    // Closes sidebar 
    if (window.location.pathname.toLowerCase().includes('/vehiclein/create')) {
        document.body.classList.add('mini-sidebar');
    }

    // Initialize Select2 globally
    if (window.jQuery && $.fn.select2) {
        $('.search-select').select2({ width: '100%' });
    }

    $('.datatable-report').each(function () {
        var $table = $(this);

        // Destroy if already initialized
        if ($.fn.DataTable.isDataTable(this)) {
            $table.DataTable().destroy();
        }



        var table = $table.DataTable({
            destroy: true,
            ordering: true,
            order: [[1, 'asc']],           // Default sort on 2nd column (Date usually)
            searching: true,
            info: true,
            lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "All"]],

            // Important settings for sorting
            orderCellsTop: false,           // Set to false if you have only one header row
            columnDefs: [
                { orderable: false, targets: -1 }        // Action column
            ],

            dom: "<'row mb-2'<'col-sm-12 d-flex align-items-center justify-content-between flex-wrap gap-3'<'d-flex align-items-center flex-wrap gap-3'Bl>>>" +
                "<'row'<'col-sm-12'tr>>" +
                "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",

            buttons: [
                { extend: 'copy', className: 'btn btn-secondary btn-sm' },
                { extend: 'csv', className: 'btn btn-secondary btn-sm' },
                { extend: 'excel', className: 'btn btn-secondary btn-sm' },
                { extend: 'pdf', className: 'btn btn-secondary btn-sm' },
                { extend: 'print', className: 'btn btn-secondary btn-sm' }
            ],

            language: {
                search: '',
                searchPlaceholder: "Search...",
                sLengthMenu: 'Show _MENU_ Entries',
                info: "Showing _START_ - _END_ of _TOTAL_ entries",
                paginate: {
                    next: '<i class="ti ti-chevron-right"></i>',
                    previous: '<i class="ti ti-chevron-left"></i>'
                }
            }
        });

        // Manual column search
        $table.find('thead input.input-dt-search').off('keyup change').on('keyup change', function () {
            var colIdx = $(this).closest('th').index();
            table.column(colIdx).search(this.value).draw();
        });
    });



});







