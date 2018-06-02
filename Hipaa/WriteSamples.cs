﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using EdiFabric.Core.Model.Edi.ErrorContexts;
using EdiFabric.Core.Model.Edi.X12;
using EdiFabric.Framework.Writers;
using EdiFabric.Rules.HIPAA_5010;

namespace EdiFabric.Sdk.Hipaa
{
    /// <summary>
    /// Runs all write samples
    /// Check Output windows for debug data
    /// </summary>
    class WriteSamples
    {
        public static void Run()
        {
            WriteSingleClaimToStream();           
        }

        /// <summary>
        /// Generate and write claim to a stream
        /// </summary>
        static void WriteSingleClaimToStream()
        {
            Debug.WriteLine("******************************");
            Debug.WriteLine(MethodBase.GetCurrentMethod().Name);
            Debug.WriteLine("******************************");

            //  1.  Construct the claim message with data from database, service or domain objects\logic.
            var claim = CreateClaim("00000001");

            //  2.  Validate it to ensure the object adheres to the rule
            //  Always skip trailer validation because all trailers are automatically generated by the writer
            MessageErrorContext errorContext;
            if (claim.IsValid(out errorContext, true))
            {
                Debug.WriteLine("Message {0} with control number {1} is valid.", errorContext.Name, errorContext.ControlNumber);

                //  3.  Write to a stream
                using (var stream = new MemoryStream())
                {
                    //  4.  Use CRLF(new line) as segment postfix for clarity
                    //  Always agree postfixes and separators with the trading partner
                    using (var writer = new X12Writer(stream, Encoding.Default, Environment.NewLine))
                    {
                        //  5.  Begin with ISA segment
                        writer.Write(CreateIsa("000011111"));
                        //  6.  Follow up with GS segment
                        writer.Write(CreateGs("111111111"));
                        //  7.  Write all transactions
                        //  Batch up as many as needed
                        writer.Write(claim);
                        //  No need to close any of the above
                    }

                    Debug.Write(LoadString(stream));
                }
            }
            else
            {
                //  The claim is invalid
                //  Report it back to the sender, log, etc.

                //  Inspect MessageErrorContext for the validation errors
                var errors = errorContext.Flatten();

                Debug.WriteLine("Message {0} with control number {1} is invalid with errors:", errorContext.Name, errorContext.ControlNumber);
                foreach (var error in errors)
                {
                    Debug.WriteLine(error);
                }
            }
        }

        /// <summary>
        /// Sample claim
        /// </summary>
        static TS837P CreateClaim(string controlNumber)
        {
            var result = new TS837P();

            result.ST = new ST();
            result.ST.TransactionSetIdentifierCode_01 = "837";
            result.ST.TransactionSetControlNumber_02 = controlNumber.PadLeft(9, '0');
            result.ST.ImplementationConventionPreference_03 = "005010X222A1";

            result.BeginningofHierarchicalTransaction = new BHT_BeginningofHierarchicalTransaction_7();
            result.BeginningofHierarchicalTransaction.HierarchicalStructureCode_01 = "0019";
            result.BeginningofHierarchicalTransaction.TransactionSetPurposeCode_02 = "00";
            result.BeginningofHierarchicalTransaction.SubmitterTransactionIdentifier_03 = "010";
            result.BeginningofHierarchicalTransaction.TransactionSetCreationDate_04 = "20170617";
            result.BeginningofHierarchicalTransaction.TransactionSetCreationTime_05 = "1741";
            result.BeginningofHierarchicalTransaction.TransactionTypeCode_06 = "CH";

            result.AllNM1 = new All_NM1_TS837P();
            result.AllNM1.Loop1000A = new Loop_1000A_TS837P();

            result.AllNM1.Loop1000A.SubmitterName = new NM1_InformationReceiverName_4();
            result.AllNM1.Loop1000A.SubmitterName.EntityIdentifierCode_01 = "41";
            result.AllNM1.Loop1000A.SubmitterName.EntityTypeQualifier_02 = "2";
            result.AllNM1.Loop1000A.SubmitterName.ResponseContactLastorOrganizationName_03 = "SUBMITTER";
            result.AllNM1.Loop1000A.SubmitterName.IdentificationCodeQualifier_08 = "46";
            result.AllNM1.Loop1000A.SubmitterName.ResponseContactIdentifier_09 = "ABC123";

            
            result.AllNM1.Loop1000A.SubmitterEDIContactInformation = new List<PER_BillingProviderContactInformation>();
            var per1 = new PER_BillingProviderContactInformation();
            per1.ContactFunctionCode_01 = "IC";
            per1.ResponseContactName_02 = "BOB SMITH";
            per1.CommunicationNumberQualifier_03 = "TE";
            per1.ResponseContactCommunicationNumber_04 = "4805551212";
            result.AllNM1.Loop1000A.SubmitterEDIContactInformation.Add(per1);

            result.AllNM1.Loop1000B = new Loop_1000B_TS837P();

            result.AllNM1.Loop1000B.ReceiverName = new NM1_ReceiverName();
            result.AllNM1.Loop1000B.ReceiverName.EntityIdentifierCode_01 = "40";
            result.AllNM1.Loop1000B.ReceiverName.EntityTypeQualifier_02 = "2";
            result.AllNM1.Loop1000B.ReceiverName.ResponseContactLastorOrganizationName_03 = "RECEIVER";
            result.AllNM1.Loop1000B.ReceiverName.IdentificationCodeQualifier_08 = "46";
            result.AllNM1.Loop1000B.ReceiverName.ResponseContactIdentifier_09 = "44556";

            result.Loop2000A = new List<Loop_2000A_TS837P>();
            var loop2000A1 = new Loop_2000A_TS837P();

            loop2000A1.BillingProviderHierarchicalLevel = new HL_BillingProviderHierarchicalLevel();
            loop2000A1.BillingProviderHierarchicalLevel.HierarchicalIDNumber_01 = "1";
            loop2000A1.BillingProviderHierarchicalLevel.HierarchicalLevelCode_03 = "20";
            loop2000A1.BillingProviderHierarchicalLevel.HierarchicalChildCode_04 = "1";

            loop2000A1.AllNM1 = new All_NM1_2_TS837P();
            loop2000A1.AllNM1.Loop2010AA = new Loop_2010AA_TS837P();
            loop2000A1.AllNM1.Loop2010AA.BillingProviderName = new NM1_BillingProviderName_2();
            loop2000A1.AllNM1.Loop2010AA.BillingProviderName.EntityIdentifierCode_01 = "85";
            loop2000A1.AllNM1.Loop2010AA.BillingProviderName.EntityTypeQualifier_02 = "2";
            loop2000A1.AllNM1.Loop2010AA.BillingProviderName.ResponseContactLastorOrganizationName_03 = "BILLING PROVIDER";
            loop2000A1.AllNM1.Loop2010AA.BillingProviderName.IdentificationCodeQualifier_08 = "XX";
            loop2000A1.AllNM1.Loop2010AA.BillingProviderName.ResponseContactIdentifier_09 = "1122334455";

            loop2000A1.AllNM1.Loop2010AA.BillingProviderAddress = new N3_AdditionalPatientInformationContactAddress();
            loop2000A1.AllNM1.Loop2010AA.BillingProviderAddress.ResponseContactAddressLine_01 = "1234 SOME ROAD";

            loop2000A1.AllNM1.Loop2010AA.BillingProviderCity = new N4_AdditionalPatientInformationContactCity();
            loop2000A1.AllNM1.Loop2010AA.BillingProviderCity.AdditionalPatientInformationContactCityName_01 = "CHICAGO";
            loop2000A1.AllNM1.Loop2010AA.BillingProviderCity.AdditionalPatientInformationContactStateCode_02 = "IL";
            loop2000A1.AllNM1.Loop2010AA.BillingProviderCity.AdditionalPatientInformationContactPostalZoneorZIPCode_03 = "606739999";

            loop2000A1.AllNM1.Loop2010AA.AllREF = new All_REF_TS837P();
            loop2000A1.AllNM1.Loop2010AA.AllREF.BillingProviderTaxIdentification = new REF_BillingProviderTaxIdentification();
            loop2000A1.AllNM1.Loop2010AA.AllREF.BillingProviderTaxIdentification.ReferenceIdentificationQualifier_01 = "EI";
            loop2000A1.AllNM1.Loop2010AA.AllREF.BillingProviderTaxIdentification.MemberGrouporPolicyNumber_02 = "999999999";

            loop2000A1.Loop2000B = new List<Loop_2000B_TS837P>();

            var loop2000B1 = new Loop_2000B_TS837P();
            loop2000B1.SubscriberHierarchicalLevel = new HL_SubscriberHierarchicalLevel();
            loop2000B1.SubscriberHierarchicalLevel.HierarchicalIDNumber_01 = "2";
            loop2000B1.SubscriberHierarchicalLevel.HierarchicalParentIDNumber_02 = "1";
            loop2000B1.SubscriberHierarchicalLevel.HierarchicalLevelCode_03 = "22";
            loop2000B1.SubscriberHierarchicalLevel.HierarchicalChildCode_04 = "0";

            loop2000B1.SubscriberInformation = new SBR_SubscriberInformation();
            loop2000B1.SubscriberInformation.PayerResponsibilitySequenceNumberCode_01 = "P";
            loop2000B1.SubscriberInformation.IndividualRelationshipCode_02 = "18";
            loop2000B1.SubscriberInformation.ClaimFilingIndicatorCode_09 = "12";

            loop2000B1.AllNM1 = new All_NM1_3_TS837P();
            loop2000B1.AllNM1.Loop2010BA = new Loop_2010BA_TS837P();
            loop2000B1.AllNM1.Loop2010BA.SubscriberName = new NM1_SubscriberName_5();
            loop2000B1.AllNM1.Loop2010BA.SubscriberName.EntityIdentifierCode_01 = "IL";
            loop2000B1.AllNM1.Loop2010BA.SubscriberName.EntityTypeQualifier_02 = "1";
            loop2000B1.AllNM1.Loop2010BA.SubscriberName.ResponseContactLastorOrganizationName_03 = "BLOGGS";
            loop2000B1.AllNM1.Loop2010BA.SubscriberName.ResponseContactFirstName_04 = "JOE";
            loop2000B1.AllNM1.Loop2010BA.SubscriberName.IdentificationCodeQualifier_08 = "MI";
            loop2000B1.AllNM1.Loop2010BA.SubscriberName.ResponseContactIdentifier_09 = "1234567890";

            loop2000B1.AllNM1.Loop2010BA.SubscriberAddress = new N3_AdditionalPatientInformationContactAddress();                
            loop2000B1.AllNM1.Loop2010BA.SubscriberAddress.ResponseContactAddressLine_01 = "1 SOME BLVD";

            loop2000B1.AllNM1.Loop2010BA.SubscriberCity = new N4_AdditionalPatientInformationContactCity();
            loop2000B1.AllNM1.Loop2010BA.SubscriberCity.AdditionalPatientInformationContactCityName_01 = "CHICAGO";
            loop2000B1.AllNM1.Loop2010BA.SubscriberCity.AdditionalPatientInformationContactStateCode_02 = "IL";
            loop2000B1.AllNM1.Loop2010BA.SubscriberCity.AdditionalPatientInformationContactPostalZoneorZIPCode_03 = "606129998";

            loop2000B1.AllNM1.Loop2010BA.SubscriberDemographicInformation = new DMG_PatientDemographicInformation();
            loop2000B1.AllNM1.Loop2010BA.SubscriberDemographicInformation.DateTimePeriodFormatQualifier_01 = "D8";
            loop2000B1.AllNM1.Loop2010BA.SubscriberDemographicInformation.DependentBirthDate_02 = "19570111";
            loop2000B1.AllNM1.Loop2010BA.SubscriberDemographicInformation.DependentGenderCode_03 = "M";

            loop2000B1.AllNM1.Loop2010BB = new Loop_2010BB_TS837P();
            loop2000B1.AllNM1.Loop2010BB.PayerName = new NM1_OtherPayerName();
            loop2000B1.AllNM1.Loop2010BB.PayerName.EntityIdentifierCode_01 = "PR";
            loop2000B1.AllNM1.Loop2010BB.PayerName.EntityTypeQualifier_02 = "2";
            loop2000B1.AllNM1.Loop2010BB.PayerName.ResponseContactLastorOrganizationName_03 = "PAYER";
            loop2000B1.AllNM1.Loop2010BB.PayerName.IdentificationCodeQualifier_08 = "PI";
            loop2000B1.AllNM1.Loop2010BB.PayerName.ResponseContactIdentifier_09 = "12345";

            loop2000B1.AllNM1.Loop2010BB.PayerAddress = new N3_AdditionalPatientInformationContactAddress();
            loop2000B1.AllNM1.Loop2010BB.PayerAddress.ResponseContactAddressLine_01 = "1 PAYER WAY";

            loop2000B1.AllNM1.Loop2010BB.PayerCity = new N4_AdditionalPatientInformationContactCity();
            loop2000B1.AllNM1.Loop2010BB.PayerCity.AdditionalPatientInformationContactCityName_01 = "ST LOUIS";
            loop2000B1.AllNM1.Loop2010BB.PayerCity.AdditionalPatientInformationContactStateCode_02 = "MO";
            loop2000B1.AllNM1.Loop2010BB.PayerCity.AdditionalPatientInformationContactPostalZoneorZIPCode_03 = "212441850";

            loop2000B1.AllNM1.Loop2010BB.AllREF = new All_REF_4_TS837P();
            loop2000B1.AllNM1.Loop2010BB.AllREF.PayerSecondaryIdentification = new List<REF_OtherPayerSecondaryIdentifier>();
            var refPayer1 = new REF_OtherPayerSecondaryIdentifier();
            refPayer1.ReferenceIdentificationQualifier_01 = "2U";
            refPayer1.MemberGrouporPolicyNumber_02 = "W1014";

            loop2000B1.AllNM1.Loop2010BB.AllREF.PayerSecondaryIdentification.Add(refPayer1);
            
            loop2000B1.Loop2300 = new List<Loop_2300_2_TS837P>();
            var loop23001 = new Loop_2300_2_TS837P();
            loop23001.ClaimInformation = new CLM_ClaimInformation_2();
            loop23001.ClaimInformation.PatientControlNumber_01 = "1000A";
            loop23001.ClaimInformation.TotalClaimChargeAmount_02 = "140";
            loop23001.ClaimInformation.HealthCareServiceLocationInformation_05 = new C023_HealthCareServiceLocationInformation_2();
            loop23001.ClaimInformation.HealthCareServiceLocationInformation_05.FacilityTypeCode_01 = "19";
            loop23001.ClaimInformation.HealthCareServiceLocationInformation_05.FacilityCodeQualifier_02 = "B";
            loop23001.ClaimInformation.HealthCareServiceLocationInformation_05.ClaimFrequencyTypeCode_03 = "1";
            loop23001.ClaimInformation.ProviderorSupplierSignatureIndicator_06 = "Y";
            loop23001.ClaimInformation.AssignmentorPlanParticipationCode_07 = "A";
            loop23001.ClaimInformation.BenefitsAssignmentCertificationIndicator_08 = "Y";
            loop23001.ClaimInformation.ReleaseofInformationCode_09 = "Y";

            loop23001.AllHI = new All_HI_2_TS837P();
            loop23001.AllHI.HealthCareDiagnosisCode = new HI_DependentHealthCareDiagnosisCode();
            loop23001.AllHI.HealthCareDiagnosisCode.HealthCareCodeInformation_01 = new C022_HealthCareCodeInformation_8();
            loop23001.AllHI.HealthCareDiagnosisCode.HealthCareCodeInformation_01.CodeListQualifierCode_01 = "ABK";
            loop23001.AllHI.HealthCareDiagnosisCode.HealthCareCodeInformation_01.IndustryCode_02 = "I10";

            loop23001.Loop2400 = new List<Loop_2400_2_TS837P>();
            var loop24001 = new Loop_2400_2_TS837P();

            loop24001.ServiceLineNumber = new LX_HeaderNumber();
            loop24001.ServiceLineNumber.AssignedNumber_01 = "1";

            loop24001.ProfessionalService = new SV1_ProfessionalService();
            loop24001.ProfessionalService.CompositeMedicalProcedureIdentifier_01 = new C003_CompositeMedicalProcedureIdentifier_12();
            loop24001.ProfessionalService.CompositeMedicalProcedureIdentifier_01.ProductorServiceIDQualifier_01 = "HC";
            loop24001.ProfessionalService.CompositeMedicalProcedureIdentifier_01.ProcedureCode_02 = "99213";
            loop24001.ProfessionalService.LineItemChargeAmount_02 = "140";
            loop24001.ProfessionalService.UnitorBasisforMeasurementCode_03 = "UN";
            loop24001.ProfessionalService.ServiceUnitCount_04 = "1";
            loop24001.ProfessionalService.CompositeDiagnosisCodePointer_07 = new C004_CompositeDiagnosisCodePointer();
            loop24001.ProfessionalService.CompositeDiagnosisCodePointer_07.DiagnosisCodePointer_01 = "1";

            loop24001.AllDTP = new All_DTP_2_TS837P();
            loop24001.AllDTP.Date = new DTP_ClaimLevelServiceDate();
            loop24001.AllDTP.Date.DateTimeQualifier_01 = "472";
            loop24001.AllDTP.Date.DateTimePeriodFormatQualifier_02 = "D8";
            loop24001.AllDTP.Date.AccidentDate_03 = "20151124";

            loop23001.Loop2400.Add(loop24001);
            loop2000B1.Loop2300.Add(loop23001);
            loop2000A1.Loop2000B.Add(loop2000B1);

            // from here

            var loop2000B2 = new Loop_2000B_TS837P();
            loop2000B2.SubscriberHierarchicalLevel = new HL_SubscriberHierarchicalLevel();
            loop2000B2.SubscriberHierarchicalLevel.HierarchicalIDNumber_01 = "3";
            loop2000B2.SubscriberHierarchicalLevel.HierarchicalParentIDNumber_02 = "1";
            loop2000B2.SubscriberHierarchicalLevel.HierarchicalLevelCode_03 = "22";
            loop2000B2.SubscriberHierarchicalLevel.HierarchicalChildCode_04 = "0";

            loop2000B2.SubscriberInformation = new SBR_SubscriberInformation();
            loop2000B2.SubscriberInformation.PayerResponsibilitySequenceNumberCode_01 = "P";
            loop2000B2.SubscriberInformation.IndividualRelationshipCode_02 = "18";
            loop2000B2.SubscriberInformation.ClaimFilingIndicatorCode_09 = "12";

            loop2000B2.AllNM1 = new All_NM1_3_TS837P();
            loop2000B2.AllNM1.Loop2010BA = new Loop_2010BA_TS837P();
            loop2000B2.AllNM1.Loop2010BA.SubscriberName = new NM1_SubscriberName_5();
            loop2000B2.AllNM1.Loop2010BA.SubscriberName.EntityIdentifierCode_01 = "IL";
            loop2000B2.AllNM1.Loop2010BA.SubscriberName.EntityTypeQualifier_02 = "1";
            loop2000B2.AllNM1.Loop2010BA.SubscriberName.ResponseContactLastorOrganizationName_03 = "BLOGGS";
            loop2000B2.AllNM1.Loop2010BA.SubscriberName.ResponseContactFirstName_04 = "FRED";
            loop2000B2.AllNM1.Loop2010BA.SubscriberName.IdentificationCodeQualifier_08 = "MI";
            loop2000B2.AllNM1.Loop2010BA.SubscriberName.ResponseContactIdentifier_09 = "9876543201";

            loop2000B2.AllNM1.Loop2010BA.SubscriberAddress = new N3_AdditionalPatientInformationContactAddress();
            loop2000B2.AllNM1.Loop2010BA.SubscriberAddress.ResponseContactAddressLine_01 = "1 ANOTHER STR";

            loop2000B2.AllNM1.Loop2010BA.SubscriberCity = new N4_AdditionalPatientInformationContactCity();
            loop2000B2.AllNM1.Loop2010BA.SubscriberCity.AdditionalPatientInformationContactCityName_01 = "CHICAGO";
            loop2000B2.AllNM1.Loop2010BA.SubscriberCity.AdditionalPatientInformationContactStateCode_02 = "IL";
            loop2000B2.AllNM1.Loop2010BA.SubscriberCity.AdditionalPatientInformationContactPostalZoneorZIPCode_03 = "606129998";

            loop2000B2.AllNM1.Loop2010BA.SubscriberDemographicInformation = new DMG_PatientDemographicInformation();
            loop2000B2.AllNM1.Loop2010BA.SubscriberDemographicInformation.DateTimePeriodFormatQualifier_01 = "D8";
            loop2000B2.AllNM1.Loop2010BA.SubscriberDemographicInformation.DependentBirthDate_02 = "19700601";
            loop2000B2.AllNM1.Loop2010BA.SubscriberDemographicInformation.DependentGenderCode_03 = "M";

            loop2000B2.AllNM1.Loop2010BB = new Loop_2010BB_TS837P();
            loop2000B2.AllNM1.Loop2010BB.PayerName = new NM1_OtherPayerName();
            loop2000B2.AllNM1.Loop2010BB.PayerName.EntityIdentifierCode_01 = "PR";
            loop2000B2.AllNM1.Loop2010BB.PayerName.EntityTypeQualifier_02 = "2";
            loop2000B2.AllNM1.Loop2010BB.PayerName.ResponseContactLastorOrganizationName_03 = "PAYER";
            loop2000B2.AllNM1.Loop2010BB.PayerName.IdentificationCodeQualifier_08 = "PI";
            loop2000B2.AllNM1.Loop2010BB.PayerName.ResponseContactIdentifier_09 = "12345";

            loop2000B2.AllNM1.Loop2010BB.PayerAddress = new N3_AdditionalPatientInformationContactAddress();
            loop2000B2.AllNM1.Loop2010BB.PayerAddress.ResponseContactAddressLine_01 = "1 PAYER WAY";

            loop2000B2.AllNM1.Loop2010BB.PayerCity = new N4_AdditionalPatientInformationContactCity();
            loop2000B2.AllNM1.Loop2010BB.PayerCity.AdditionalPatientInformationContactCityName_01 = "ST LOUIS";
            loop2000B2.AllNM1.Loop2010BB.PayerCity.AdditionalPatientInformationContactStateCode_02 = "MO";
            loop2000B2.AllNM1.Loop2010BB.PayerCity.AdditionalPatientInformationContactPostalZoneorZIPCode_03 = "212441850";

            loop2000B2.AllNM1.Loop2010BB.AllREF = new All_REF_4_TS837P();
            loop2000B2.AllNM1.Loop2010BB.AllREF.PayerSecondaryIdentification = new List<REF_OtherPayerSecondaryIdentifier>();
            var refPayer2 = new REF_OtherPayerSecondaryIdentifier();
            refPayer2.ReferenceIdentificationQualifier_01 = "2U";
            refPayer2.MemberGrouporPolicyNumber_02 = "W1014";

            loop2000B2.AllNM1.Loop2010BB.AllREF.PayerSecondaryIdentification.Add(refPayer2);

            loop2000B2.Loop2300 = new List<Loop_2300_2_TS837P>();
            var loop23002 = new Loop_2300_2_TS837P();
            loop23002.ClaimInformation = new CLM_ClaimInformation_2();
            loop23002.ClaimInformation.PatientControlNumber_01 = "1001A";
            loop23002.ClaimInformation.TotalClaimChargeAmount_02 = "140";
            loop23002.ClaimInformation.HealthCareServiceLocationInformation_05 = new C023_HealthCareServiceLocationInformation_2();
            loop23002.ClaimInformation.HealthCareServiceLocationInformation_05.FacilityTypeCode_01 = "19";
            loop23002.ClaimInformation.HealthCareServiceLocationInformation_05.FacilityCodeQualifier_02 = "B";
            loop23002.ClaimInformation.HealthCareServiceLocationInformation_05.ClaimFrequencyTypeCode_03 = "1";
            loop23002.ClaimInformation.ProviderorSupplierSignatureIndicator_06 = "Y";
            loop23002.ClaimInformation.AssignmentorPlanParticipationCode_07 = "A";
            loop23002.ClaimInformation.BenefitsAssignmentCertificationIndicator_08 = "Y";
            loop23002.ClaimInformation.ReleaseofInformationCode_09 = "Y";

            loop23002.AllHI = new All_HI_2_TS837P();
            loop23002.AllHI.HealthCareDiagnosisCode = new HI_DependentHealthCareDiagnosisCode();
            loop23002.AllHI.HealthCareDiagnosisCode.HealthCareCodeInformation_01 = new C022_HealthCareCodeInformation_8();
            loop23002.AllHI.HealthCareDiagnosisCode.HealthCareCodeInformation_01.CodeListQualifierCode_01 = "ABK";
            loop23002.AllHI.HealthCareDiagnosisCode.HealthCareCodeInformation_01.IndustryCode_02 = "I10";

            loop23002.Loop2400 = new List<Loop_2400_2_TS837P>();
            var loop24002 = new Loop_2400_2_TS837P();

            loop24002.ServiceLineNumber = new LX_HeaderNumber();
            loop24002.ServiceLineNumber.AssignedNumber_01 = "1";

            loop24002.ProfessionalService = new SV1_ProfessionalService();
            loop24002.ProfessionalService.CompositeMedicalProcedureIdentifier_01 = new C003_CompositeMedicalProcedureIdentifier_12();
            loop24002.ProfessionalService.CompositeMedicalProcedureIdentifier_01.ProductorServiceIDQualifier_01 = "HC";
            loop24002.ProfessionalService.CompositeMedicalProcedureIdentifier_01.ProcedureCode_02 = "99213";
            loop24002.ProfessionalService.LineItemChargeAmount_02 = "140";
            loop24002.ProfessionalService.UnitorBasisforMeasurementCode_03 = "UN";
            loop24002.ProfessionalService.ServiceUnitCount_04 = "1";
            loop24002.ProfessionalService.CompositeDiagnosisCodePointer_07 = new C004_CompositeDiagnosisCodePointer();
            loop24002.ProfessionalService.CompositeDiagnosisCodePointer_07.DiagnosisCodePointer_01 = "1";

            loop24002.AllDTP = new All_DTP_2_TS837P();
            loop24002.AllDTP.Date = new DTP_ClaimLevelServiceDate();
            loop24002.AllDTP.Date.DateTimeQualifier_01 = "472";
            loop24002.AllDTP.Date.DateTimePeriodFormatQualifier_02 = "D8";
            loop24002.AllDTP.Date.AccidentDate_03 = "20151124";

            loop23002.Loop2400.Add(loop24002);
            loop2000B2.Loop2300.Add(loop23002);
            loop2000A1.Loop2000B.Add(loop2000B2);

            result.Loop2000A.Add(loop2000A1);

            return result;
        }

        /// <summary>
        /// Sample GS
        /// </summary>
        static GS CreateGs(string controlNumber)
        {
            return new GS
            {
                //  Functional ID Code
                CodeIdentifyingInformationType_1 = "IN",
                //  Application Senders Code
                SenderIDCode_2 = "RECEIVER1",
                //  Application Receivers Code
                ReceiverIDCode_3 = "SENDER1",
                //  Date
                Date_4 = DateTime.Now.Date.ToString("yyMMdd"),
                //  Time
                Time_5 = DateTime.Now.TimeOfDay.ToString("hhmm"),
                //  Group Control Number
                //  Must be unique to both partners for this interchange
                GroupControlNumber_6 = controlNumber.PadLeft(9, '0'),
                //  Responsible Agency Code
                TransactionTypeCode_7 = "X",
                //  Version/Release/Industry id code
                VersionAndRelease_8 = "005010X222A1"
            };
        }

        /// <summary>
        /// Sample ISA
        /// </summary>
        static ISA CreateIsa(string controlNumber)
        {
            return new ISA
            {
                //  Authorization Information Qualifier
                AuthorizationInformationQualifier_1 = "00",
                //  Authorization Information
                AuthorizationInformation_2 = "          ",
                //  Security Information Qualifier
                SecurityInformationQualifier_3 = "00",
                //  Security Information
                SecurityInformation_4 = "          ",
                //  Interchange ID Qualifier
                SenderIDQualifier_5 = "14",
                //  Interchange Sender
                InterchangeSenderID_6 = "RECEIVER1      ",
                //  Interchange ID Qualifier
                ReceiverIDQualifier_7 = "16",
                //  Interchange Receiver
                InterchangeReceiverID_8 = "SENDER1        ",
                //  Date
                InterchangeDate_9 = DateTime.Now.Date.ToString("yyMMdd"),
                //  Time
                InterchangeTime_10 = DateTime.Now.TimeOfDay.ToString("hhmm"),
                //  Standard identifier
                InterchangeControlStandardsIdentifier_11 = "U",
                //  Interchange Version ID
                //  This is the ISA version and not the transaction sets versions
                InterchangeControlVersionNumber_12 = "00501",
                //  Interchange Control Number
                InterchangeControlNumber_13 = controlNumber.PadLeft(9, '0'),
                //  Acknowledgment Requested (0 or 1)
                AcknowledgementRequested_14 = "1",
                //  Test Indicator
                UsageIndicator_15 = "T",
            };
        }

        static string LoadString(Stream stream)
        {
            stream.Position = 0;
            using (var reader = new StreamReader(stream, Encoding.Default))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
