﻿(function($) {
    app.modals.CreateOrEditLanguageModal = function () {

        var _modalManager;
        var _languageService = abp.services.app.language;
        var _$languageInformationForm = null;



        function formatState(state) {
           
            if (!state.id) {
                return state.text;
            }   
            var $state = $(
                '<span class="btn btn-icon"><i class="famfamfam-flags ' + state.text+' "style="margin-right: 5px;"></i>' + state.text + '</span > '
            );
            return $state;
        }

        this.init = function(modalManager) {
            _modalManager = modalManager;

            _modalManager.getModal()
                .find('#LanguageNameEdit')
                .select2({ theme: 'bootstrap4',});

            _modalManager.getModal()
                .find('#LanguageIconEdit')
                .select2({
                    theme: 'bootstrap4',
                    templateResult: formatState
       
                });

            _$languageInformationForm = _modalManager.getModal().find('form[name=LanguageInformationsForm]');
        };

        this.save = function () {
            var language = _$languageInformationForm.serializeFormToObject();

            _modalManager.setBusy(true);
            _languageService.createOrUpdateLanguage({
                language: language
            }).done(function () {
                abp.notify.info(app.localize('SavedSuccessfully'));
                _modalManager.close();
                abp.event.trigger('app.createOrEditLanguageModalSaved');
            }).always(function () {
                _modalManager.setBusy(false);
            });
        };
    };
})(jQuery);