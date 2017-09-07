(function () {
    'use strict';

    var controllerId = 'createListingCampaignUrl';

    // TODO: replace app with your module name
    angular.module('app').controller(controllerId, ['$location', 'docketService', 'common', createListingCampaignUrl]);

    function createListingCampaignUrl($location, docketService, common) {
        var getLogFn = common.logger.getLogFn;
        var log = getLogFn(controllerId);
        var vm = this;

        vm.title = 'listing campaign url';
        vm.listings = [];
        vm.socialmediasites = [];

        vm.selectedListing = null;
        vm.selectedSocialMediaSite = null;

        vm.listingCampaignUrls = [];
        vm.urlMax = 5;

        //functions
        vm.activate = activate;
        vm.backToDocket = backToDocket;
        vm.saveListingCampaignUrls = saveListingCampaignUrls;
        vm.addUrlToCampaign = addUrlToCampaign;
        vm.listingChanged = listingChanged;
        vm.socialMediaSiteChanged = socialMediaSiteChanged;

        activate();

        function activate() {
            common.activateController([loadListings(), loadSocialMediaSites()], controllerId).then(function () {
                log('Activated createListingCampaignUrl View');
            });

            function loadListings() {
                return docketService.getDocketListings().then(function (data) {
                    vm.listings = data;
                });
            }

            function loadSocialMediaSites() {
                return docketService.getSocialMediaSites().then(function (data) {
                    vm.socialmediasites = data;
                });
            }
        }

        function backToDocket() { $location.path("docket"); }

        function saveListingCampaignUrls() {
            return docketService.saveCampaignRecorderListings(vm.listingCampaignUrls)
                  .then(function () {
                      $location.path('docket/docket');
                  });
        }

        function addUrlToCampaign() {
            if (vm.listingCampaignUrls.length < 5)
                vm.listingCampaignUrls.push({});

            for (var i = 0; i < vm.listingCampaignUrls.length; i++) {
                // Update all the lsiting ids to the currently selected one in case the user has changed it.
                vm.listingCampaignUrls[i].listingid = vm.selectedListing.id;
                vm.listingCampaignUrls[i].accountId = vm.selectedListing.accountId;
                vm.listingCampaignUrls[i].active = true;

                // If we have not already bound the socialmediasites list to this particular record, then copy from our master list and assign it.
                if (vm.listingCampaignUrls[i].socialmediasites == null) {
                    vm.listingCampaignUrls[i].socialmediasites = angular.copy(vm.socialmediasites);
                }
            }
        };

        // Handles when the user selects a different social media site on an individual row in the collection.
        // Looks up the SocialMediaAccount by SocialMediaSiteId and if one is found, assigns the url/username/password to the appropriate part of the model
        function socialMediaSiteChanged(index) {
            var socialMediaSiteId = vm.listingCampaignUrls[index].socialMediaSiteId;

            return docketService.getSocialMediaAccountBySocialMediaSiteId(socialMediaSiteId)
                .then(function(data) {
                    if (data != null) {
                        vm.listingCampaignUrls[index].userName = data[0].username;
                        vm.listingCampaignUrls[index].password = data[0].password;
                        vm.listingCampaignUrls[index].url = data[0].url;
                    }
            });
        }

        // Handles the listing being change and loads all associated urls if they already exist so the user can make changes.
        function listingChanged() {
            if (vm.selectedListing === undefined)
                return null;

            var listingId = vm.selectedListing.id;

            return docketService.getCampaignUrlsByListingId(listingId)
                .then(function (data) {
                vm.listingCampaignUrls = data;
            });
        };
    }
})();
